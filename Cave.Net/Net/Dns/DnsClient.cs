using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cave.Collections.Generic;
using Cave.IO;
using Cave.Security;

namespace Cave.Net.Dns;

/// <summary>Provides a client for querying dns records.</summary>
public class DnsClient
{
    #region Private Fields

    static readonly string[] NoSuffix = ["."];
    static readonly char[] ResolvSeparator = [' ', '\t'];

    IPAddress[]? servers;

    #endregion Private Fields

    #region Private Methods

    static void LoadAnAppleAndBiteIt(Set<IPAddress> result)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/sbin/scutil",
                Arguments = "--dns",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(3000))
        {
            process.Kill();
            return;
        }
        var regex = new Regex(@"nameserver\[[0-9]+\] : ([^\s]+)");
        var matches = regex.Matches(output);
        foreach (Match match in matches)
        {
            if (IPAddress.TryParse(match.Groups[1].Value, out var address))
            {
                result.Include(address);
            }
        }
    }

    static void LoadEtcResolvConf(Set<IPAddress> result)
    {
        if (Platform.IsMicrosoft) return;
        if (File.Exists("/etc/resolv.conf"))
        {
            try
            {
                foreach (var line in File.ReadAllLines("/etc/resolv.conf"))
                {
                    var s = line.Trim();
                    var i = s.IndexOf('#');
                    if (i > -1)
                    {
                        s = s[..i];
                    }

                    var parts = s.Split(ResolvSeparator, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                    {
                        if (parts[0].ToUpperInvariant() != "NAMESERVER")
                        {
                            continue;
                        }

                        for (var n = 1; n < parts.Length; n++)
                        {
                            if (IPAddress.TryParse(parts[n], out var addr))
                            {
                                result.Include(addr);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Error reading nameserver configuration.", ex);
            }
        }
    }

    static DnsResponse SelectBestResponse(IEnumerable<DnsResponse> responses, IEnumerable<Exception> errors)
    {
        var answers = responses.Where(r => r != null).ToList();
        return
            answers.FirstOrDefault(r => r?.ResponseCode == DnsResponseCode.NoError) ??
            answers.FirstOrDefault(r => r?.Answers.Count > 0) ??
            answers.FirstOrDefault() ??
            throw new AggregateException("Could not reach any dns server!", errors.Where(e => e is not null));
    }

    bool QueryAllServers(DnsQuery query, Func<DnsResponse, bool>? completed, out IList<DnsResponse> responses, out IList<Exception> exceptions)
    {
        if (Servers.Length == 0)
        {
            throw new("No Servers defined for query!");
        }
        var maxTasks = Servers.Length * 2;
        var results = new object[maxTasks];
        var resultNumber = 0;
        var tasks = new Dictionary<Task, string>(maxTasks);
        var skipUdp = query.Length > 512;
        var done = false;

        void Query(IPAddress server, bool useTcp)
        {
            try
            {
                var response = QuerySingleServer(query, server, useTcp) ?? throw new("No response after query single server!");
                lock (results)
                {
                    results[resultNumber++] = response;
                }
                if (completed != null)
                {
                    done |= completed(response);
                }
            }
            catch (Exception ex)
            {
                lock (results)
                {
                    results[resultNumber++] = ex;
                }
            }
        }

        foreach (var server in Servers)
        {
            if (!skipUdp && UseUdp)
            {
                tasks.Add(Task.Factory.StartNew(() => Query(server, false)), $"{query} @udp://{server}");
            }
            if (skipUdp || UseTcp)
            {
                tasks.Add(Task.Factory.StartNew(() => Query(server, true)), $"{query} @tcp://{server}");
            }
        }
        if (tasks.Count == 0)
        {
            throw new("Query could not be sent to any servers. Check UseTcp, Servers and Query.Length!");
        }

        while (!done)
        {
            var tasksRunning = tasks.Keys.Where(t => !t.IsCompleted).ToArray();
            if (tasksRunning.Length == 0)
            {
                break;
            }
            Task.WaitAny(tasksRunning);
        }
        lock (results)
        {
            exceptions = results.Select(r => r is Exception ex ? ex : null).Where(r => r is not null).ToList()!;
            responses = results.Select(r => r is DnsResponse d ? d : null).Where(r => r is not null).ToList()!;
            if ((exceptions.Count == 0) && (responses.Count == 0))
            {
                throw new("No exceptions and no responses received. This indicates a fatal bug at DnsClient!");
            }
        }
        return done;
    }

    DnsResponse QuerySingleServer(DnsQuery query, IPAddress server, bool useTcp)
    {
        var messageID = RNG.UInt16;
        var message = query.ToArray(messageID);
        var response = useTcp ? QueryTcp(server, message) : QueryUdp(server, message);
        return response.TransactionID != messageID
            ? throw new InvalidDataException("Invalid message ID received!")
            : (response.Queries.Count != 1) || (response.Queries[0] != query)
                ? throw new InvalidDataException("Invalid answer received!")
                : response;
    }

    DnsResponse QueryTcp(IPAddress srv, byte[] query)
    {
        var timeout = Math.Max(100, (int)QueryTimeout.TotalMilliseconds);
        TcpAsyncClient? tcp = null;
        try
        {
            tcp = new()
            {
                ConnectTimeout = timeout
            };
            tcp.Connect(srv, Port);
            tcp.SendTimeout = timeout;
            tcp.ReceiveTimeout = timeout;
            tcp.Stream.SendOnFlush = true;
            var writer = new DataWriter(tcp.Stream, endian: EndianType.BigEndian);
            writer.Write((ushort)query.Length);
            writer.Write(query);
            writer.Flush();
            var reader = new DataReader(tcp.Stream, endian: EndianType.BigEndian);
            var length = reader.ReadUInt16();
            var data = reader.ReadBytes(length);
            return new(srv, data);
        }
        finally
        {
            tcp?.Close();
        }
    }

    DnsResponse QueryUdp(IPAddress srv, byte[] query)
    {
        if (query.Length > 512)
        {
            throw new ArgumentException("Udp query may not exceed 512 bytes!");
        }
        var timeout = Math.Max(100, (int)QueryTimeout.TotalMilliseconds);
        using var udp = new UdpClient(srv.AddressFamily);
        udp.Connect(srv, Port);
        udp.Client.SendTimeout = timeout;
        udp.Client.ReceiveTimeout = timeout;
        udp.Send(query, query.Length);
        var remote = new IPEndPoint(IPAddress.Any, 0);
        var data = udp.Receive(ref remote);
        var response = new DnsResponse(srv, data);
        return response;
    }

    #endregion Private Methods

    #region Public Properties

    /// <summary>Gets a default instance of the DnsClient, which uses the cisco open dns and a query timeout of 5 seconds.</summary>
    public static DnsClient CiscoOpenDns => new() { Servers = GetPulicDnsServers(DnsServerSelection.CiscoOpenDns) };

    /// <summary>Gets a default instance of the DnsClient, which uses the quad 9 dns and a query timeout of 5 seconds.</summary>
    public static DnsClient Cloudflare => new() { Servers = GetPulicDnsServers(DnsServerSelection.Cloudflare) };

    /// <summary>Gets a default instance of the DnsClient, which uses the configured dns servers of the executing computer and a query timeout of 5 seconds.</summary>
    public static DnsClient Default { get; } = new();

    /// <summary>Gets a default instance of the DnsClient, which uses the dns watch and a query timeout of 5 seconds.</summary>
    public static DnsClient DnsWatch => new() { Servers = GetPulicDnsServers(DnsServerSelection.DnsWatch) };

    /// <summary>Gets a default instance of the DnsClient, which uses the google dns and a query timeout of 5 seconds.</summary>
    public static DnsClient Google => new() { Servers = GetPulicDnsServers(DnsServerSelection.Google) };

    /// <summary>Gets a default instance of the DnsClient, which uses the quad 9 dns and a query timeout of 5 seconds.</summary>
    public static DnsClient Quad9 => new() { Servers = GetPulicDnsServers(DnsServerSelection.Quad9) };

    /// <summary>Gets or sets the port used for all connections of this client.</summary>
    /// <value>The port.</value>
    public ushort Port { get; set; } = 53;

    /// <summary>Gets or sets the query timeout (default = 5s).</summary>
    public TimeSpan QueryTimeout { get; set; } = new(5 * TimeSpan.TicksPerSecond);

    /// <summary>Gets or sets the search suffixes for short names.</summary>
    public string[]? SearchSuffixes { get; set; }

    /// <summary>Gets or sets the servers.</summary>
    /// <value>The servers.</value>
    public IPAddress[] Servers { get => servers ??= Default.servers ?? GetDefaultDnsServers(); set => servers = value; }

    /// <summary>Gets a value indicating whether [use random case].</summary>
    /// <value><c>true</c> if [use random case]; otherwise, <c>false</c>.</value>
    /// <remarks><see href="https://tools.ietf.org/html/draft-vixie-dnsext-dns0x20-00"/>.</remarks>
    public bool UseRandomCase { get; set; }

    /// <summary>Gets or sets a value indicating whether queries can be sent using TCP.</summary>
    public bool UseTcp { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether queries can be sent using UDP.</summary>
    public bool UseUdp { get; set; } = true;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Gets a list of the local configured DNS servers.</summary>
    /// <returns>Returns a array of <see cref="IPAddress"/> instances.</returns>
    public static IPAddress[] GetDefaultDnsServers()
    {
        var result = new Set<IPAddress>();
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((nic.OperationalStatus == OperationalStatus.Up) && (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                {
                    result.IncludeRange(nic.GetIPProperties().DnsAddresses);
                }
            }
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("Error reading nameserver configuration.", ex);
        }

        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
                break;

            case PlatformID.MacOSX:
                LoadAnAppleAndBiteIt(result);
                break;

            default:
                LoadEtcResolvConf(result);
                break;
        }

        if (result.Count == 0)
        {
            Trace.TraceWarning("Cannot use the default DNS servers of this system. Using public dns.");
            return GetPulicDnsServers();
        }

        return [.. result.OrderPrivateFirst()];
    }

    /// <summary>Gets a list of public DNS servers.</summary>
    public static IPAddress[] GetPulicDnsServers() => GetPulicDnsServers(DnsServerSelection.Everything);

    /// <summary>Gets a list of public DNS servers.</summary>
    public static IPAddress[] GetPulicDnsServers(DnsServerSelection selection)
    {
        List<IPAddress> result = new();
        if (selection == 0) selection = DnsServerSelection.Everything;
        var v4 = selection.HasFlag(DnsServerSelection.V4);
        var v6 = selection.HasFlag(DnsServerSelection.V6);
        if (!v4 && !v6) { v4 = true; v6 = true; }

        if (selection.HasFlag(DnsServerSelection.DnsWatch))
        {
            if (v4)
            {
                result.Add(IPAddress.Parse("84.200.69.80"));
                result.Add(IPAddress.Parse("84.200.70.40"));
            }
            if (v6)
            {
                result.Add(IPAddress.Parse("2001:1608:10:25::1c04:b12f"));
                result.Add(IPAddress.Parse("2001:1608:10:25::1c04:b12c"));
            }
        }

        if (selection.HasFlag(DnsServerSelection.CiscoOpenDns))
        {
            if (v4)
            {
                result.Add(IPAddress.Parse("208.67.222.222"));
                result.Add(IPAddress.Parse("208.67.220.220"));
            }
            if (v6)
            {
                result.Add(IPAddress.Parse("2620:119:35::35"));
                result.Add(IPAddress.Parse("2620:119:53::53"));
            }
        }

        if (selection.HasFlag(DnsServerSelection.Quad9))
        {
            if (v4)
            {
                result.Add(IPAddress.Parse("9.9.9.9"));
                result.Add(IPAddress.Parse("149.112.112.112"));
            }
            if (v6)
            {
                result.Add(IPAddress.Parse("2620:fe::9"));
                result.Add(IPAddress.Parse("2620:fe::fe"));
            }
        }

        if (selection.HasFlag(DnsServerSelection.Cloudflare))
        {
            if (v4)
            {
                result.Add(IPAddress.Parse("1.1.1.1"));
                result.Add(IPAddress.Parse("1.0.0.1"));
            }
            if (v6)
            {
                result.Add(IPAddress.Parse("2606:4700:4700::1001"));
                result.Add(IPAddress.Parse("2606:4700:4700::1111"));
            }
        }

        if (selection.HasFlag(DnsServerSelection.Google))
        {
            if (v4)
            {
                result.Add(IPAddress.Parse("8.8.4.4"));
                result.Add(IPAddress.Parse("8.8.8.8"));
            }
            if (v6)
            {
                result.Add(IPAddress.Parse("2001:4860:4860::8844"));
                result.Add(IPAddress.Parse("2001:4860:4860::8888"));
            }
        }

        return [.. result];
    }

    /// <summary>Returns the Internet Protocol (IP) addresses for the specified host.</summary>
    /// <param name="domainName">The host or domain name to resolve</param>
    /// <returns>Returns all ip addresses of the specified domain or host</returns>
    public IEnumerable<IPAddress> GetHostAddresses(DomainName domainName)
    {
        if (domainName.IsLocalhost)
        {
            foreach (var address in new[] { IPAddress.Loopback, IPAddress.IPv6Loopback })
            {
                yield return address;
            }
            yield break;
        }

        Queue<IPAddress> addresses = new();
        var done = 0;
        bool GotOne(DnsResponse response)
        {
            foreach (var answer in response.Answers)
            {
                if (answer.RecordType is DnsRecordType.A or DnsRecordType.AAAA && Equals(answer.Name, domainName) && IPAddress.TryParse($"{answer.Value}", out var address))
                {
                    lock (addresses)
                    {
                        addresses.Enqueue(address);
                        Monitor.Pulse(addresses);
                    }
                }
            }
            return true;
        }
        void RunQuery(DnsRecordType type)
        {
            QueryAllServers(new() { Name = domainName, RecordType = type, RecordClass = DnsRecordClass.IN, }, GotOne, out var responses, out var exceptions);
            lock (addresses)
            {
                done++;
                Monitor.Pulse(addresses);
            }
        }
        Task.Factory.StartNew(() => RunQuery(DnsRecordType.A));
        Task.Factory.StartNew(() => RunQuery(DnsRecordType.AAAA));
        for (; ; )
        {
            lock (addresses)
            {
                if (addresses.Count > 0) yield return addresses.Dequeue();
                else if (done == 2) break;
                Monitor.Wait(addresses);
            }
        }
    }

    /// <summary>Resolves a host name to an IPHostEntry instance.</summary>
    /// <param name="hostname">Hostname to resolve</param>
    /// <returns>An IPHostEntry instance that contains address information about the host specified.</returns>
    public IPHostEntry GetHostEntry(DomainName hostname)
    {
        var entry = new IPHostEntry();
        var responses = ResolveAll(hostname);
        return new()
        {
            HostName = hostname.ToString(),
            Aliases = responses.SelectMany(r => r.GetNames()).Select(n => n.ToString()).ToArray(),
            AddressList = responses.SelectMany(r => r.GetAddresses()).ToArray(),
        };
    }

    /// <summary>Resolves a host name or IP address to an IPHostEntry instance.</summary>
    /// <param name="address">IPAdress to resolve</param>
    /// <returns>An IPHostEntry instance that contains address information about the address specified</returns>
    public IPHostEntry GetHostEntry(IPAddress address)
    {
        var entries = new List<IPHostEntry>();
        var arpa = address.GetReverseLookupZone();
        var arpaResponse = Resolve(arpa, DnsRecordType.PTR);
        if (arpaResponse.ResponseCode == DnsResponseCode.NoError)
        {
            var names = arpaResponse.Answers.Select(a => a.Name).Distinct();
            Parallel.ForEach(names, name =>
            {
                var entry = GetHostEntry(name!);
                lock (entries) entries.Add(entry);
            });
        }
        return new()
        {
            AddressList = entries.SelectMany(e => e.AddressList).Distinct().ToArray(),
            Aliases = entries.SelectMany(e => e.Aliases).Distinct().ToArray(),
            HostName = address.ToString(),
        };
    }

    /// <summary>Queries the dns servers for the specified records.</summary>
    /// <remarks>This method works parallel and returns the first result of any <see cref="Servers"/>.</remarks>
    /// <param name="domainName">Domain, that should be queried.</param>
    /// <param name="recordType">Type the should be queried.</param>
    /// <param name="recordClass">Class the should be queried.</param>
    /// <param name="flags">Options for the query.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    public DnsResponse Resolve(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
    {
        if (domainName.IsLocalhost) throw new InvalidOperationException("Do not lookup localhost via dns!");
        if (domainName.Parts.Length == 1)
        {
            return ResolveWithSearchSuffix(domainName, recordType, recordClass, flags);
        }
        return Resolve(new DnsQuery() { Name = domainName, RecordType = recordType, RecordClass = recordClass, Flags = flags });
    }

    /// <summary>Queries the dns servers for the specified records.</summary>
    /// <remarks>This method works parallel and returns the first result of any <see cref="Servers"/>.</remarks>
    /// <param name="query">The query.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    /// <exception cref="InvalidOperationException">Query to big for UDP transmission. Enable UseTcp.</exception>
    /// <exception cref="Exception">No valid response received.</exception>
    /// <exception cref="AggregateException">Could not reach any dns server.</exception>
    public DnsResponse Resolve(DnsQuery query)
    {
        if (query.Name is null)
        {
            throw new ArgumentException("Query.Name must be provided");
        }
        if (query.Name.IsLocalhost) throw new InvalidOperationException("Do not lookup localhost via dns!");

        var skipUdp = query.Length > 512;
        if (skipUdp)
        {
            if (!UseTcp)
            {
                throw new InvalidOperationException("Query to big for UDP transmission. Enable UseTcp!");
            }
        }

        if (QueryAllServers(query, r => r.ResponseCode == DnsResponseCode.NoError, out var responses, out var exceptions))
        {
            return SelectBestResponse(responses, exceptions);
        }
        if (exceptions.Any())
        {
            throw new AggregateException("Could not complete query.", exceptions);
        }
        var ex = new Exception("No valid response received.");
        ex.Data.Add("DnsResponse.Count", responses.Count);
        for (var i = 0; i < responses.Count; i++)
        {
            ex.Data.Add($"DnsResponse[{i}]", $"{responses[i]}");
        }
        throw ex;
    }

    /// <summary>Queries the dns servers for the specified records.</summary>
    /// <remarks>This method works parallel and returns all results received from all <see cref="Servers"/>.</remarks>
    /// <param name="domainName">Domain, that should be queried.</param>
    /// <param name="recordType">Type the should be queried.</param>
    /// <param name="recordClass">Class the should be queried.</param>
    /// <param name="flags">Options for the query.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    public IList<DnsResponse> ResolveAll(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
        => ResolveAll(new DnsQuery() { Name = domainName, RecordType = recordType, RecordClass = recordClass, Flags = flags });

    /// <summary>Queries the dns servers for the specified records.</summary>
    /// <remarks>This method works parallel and returns all results received from all <see cref="Servers"/>.</remarks>
    /// <param name="query">The query.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentException">Name must be provided.</exception>
    /// <exception cref="InvalidOperationException">Query to big for UDP transmission. Enable UseTcp.</exception>
    /// <exception cref="AggregateException">Could not reach any dns server.</exception>
    public IList<DnsResponse> ResolveAll(DnsQuery query)
    {
        if (query.Name is null)
        {
            throw new ArgumentException("Query.Name has to be provided!");
        }
        if (query.Name.IsLocalhost) throw new InvalidOperationException("Do not lookup localhost via dns!");

        var skipUdp = query.Length > 512;
        if (skipUdp)
        {
            if (!UseTcp)
            {
                throw new InvalidOperationException("Query to big for UDP transmission. Enable UseTcp!");
            }
        }

        if (QueryAllServers(query, null, out var responses, out var exceptions) || responses.Any())
        {
            return responses;
        }
        throw new AggregateException("Could not complete query.", exceptions);
    }

    /// <summary>Queries the dns servers for the specified records using all <see cref="SearchSuffixes"/>.</summary>
    /// <remarks>This method works parallel and returns the first result of any <see cref="Servers"/>.</remarks>
    /// <param name="domainName">Domain, that should be queried.</param>
    /// <param name="recordType">Type the should be queried.</param>
    /// <param name="recordClass">Class the should be queried.</param>
    /// <param name="flags">Options for the query.</param>
    /// <returns>The first successful response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
    /// <exception cref="AggregateException">Could not reach any dns server.</exception>
    public IList<DnsResponse> ResolveAllWithSearchSuffix(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
    {
        if (domainName.IsLocalhost) throw new InvalidOperationException("Do not lookup localhost via dns!");
        SearchSuffixes ??= NoSuffix.Concat(NetworkInterface.GetAllNetworkInterfaces().Select(i => i.GetIPProperties().DnsSuffix)).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
        var exceptions = new Exception[SearchSuffixes.Length];
        var responses = new DnsResponse[SearchSuffixes.Length];

        void Query(object? state)
        {
            if (state is not int n) return;
            try
            {
                responses[n] = Resolve(new()
                {
                    Name = $"{domainName}.{SearchSuffixes[n]}".Trim('.'),
                    RecordType = recordType,
                    RecordClass = recordClass,
                    Flags = flags
                });
            }
            catch (Exception ex)
            {
                exceptions[n] = ex;
            }
        }

        var tasks = new Task[SearchSuffixes.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            object parameter = i;
            tasks[i] = Task.Factory.StartNew(Query, parameter);
        }

        Task.WaitAll(tasks);
        return [.. responses.Where(r => r is not null)];
    }

    /// <summary>Queries the dns servers for the specified records using all <see cref="SearchSuffixes"/>.</summary>
    /// <remarks>This method works parallel and returns the first result of any <see cref="Servers"/>.</remarks>
    /// <param name="computerNames">Computernames, that should be queried.</param>
    /// <param name="recordType">Type the should be queried.</param>
    /// <param name="recordClass">Class the should be queried.</param>
    /// <param name="flags">Options for the query.</param>
    /// <returns>The first successful response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
    /// <exception cref="AggregateException">Could not reach any dns server.</exception>
    public IList<DnsResponse> ResolveAllWithSearchSuffix(IList<string> computerNames, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
    {
        SearchSuffixes ??= NoSuffix.Concat(NetworkInterface.GetAllNetworkInterfaces().Select(i => i.GetIPProperties().DnsSuffix)).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
        var itemCount = computerNames.Count;
        var exceptions = new Exception[itemCount * SearchSuffixes.Length];
        var responses = new DnsResponse[itemCount * SearchSuffixes.Length];

        void Query(object? state)
        {
            var parts = $"{state}".Split(';');
            if (parts.Length != 2 || !int.TryParse(parts[0], out var index)) return;
            try
            {
                responses[index] = Resolve(new()
                {
                    Name = parts[1].Trim('.'),
                    RecordType = recordType,
                    RecordClass = recordClass,
                    Flags = flags
                });
            }
            catch (Exception ex)
            {
                exceptions[index] = ex;
            }
        }

        var tasks = new Task[itemCount * SearchSuffixes.Length];
        var i = 0;
        foreach (var suffix in SearchSuffixes)
        {
            foreach (var computerName in computerNames)
            {
                DomainName domain = $"{computerName}.{suffix}";
                if (domain.IsLocalhost) throw new InvalidOperationException("Do not lookup localhost via dns!");
                object parameter = $"{i++};{domain}";
                tasks[i] = Task.Factory.StartNew(Query, parameter);
            }
        }
        Task.WaitAll(tasks);
        return [.. responses.Where(r => r is not null)];
    }

    /// <summary>Queries the dns servers for the specified ipadress returning matching PTR records.</summary>
    /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout"/> per <see cref="Servers"/>.</remarks>
    /// <param name="address">Address, that should be queried.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    public DnsResponse ResolveSequential(IPAddress address)
        => ResolveSequential(address.GetReverseLookupZone(), DnsRecordType.PTR);

    /// <summary>Queries the dns servers for the specified records.</summary>
    /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout"/> per <see cref="Servers"/>.</remarks>
    /// <param name="domainName">Domain, that should be queried.</param>
    /// <param name="recordType">Type the should be queried.</param>
    /// <param name="recordClass">Class the should be queried.</param>
    /// <param name="flags">Options for the query.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    public DnsResponse ResolveSequential(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
        => ResolveSequential(new DnsQuery { Name = domainName, RecordType = recordType, RecordClass = recordClass, Flags = flags });

    /// <summary>Queries the dns servers for the specified records.</summary>
    /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout"/> per <see cref="Servers"/>.</remarks>
    /// <param name="query">The query.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
    /// <exception cref="AggregateException">Could not reach any dns server.</exception>
    public DnsResponse ResolveSequential(DnsQuery query)
        => ResolveSequential(query, r => r.ResponseCode == DnsResponseCode.NoError);

    /// <summary>Queries the dns servers for the specified records.</summary>
    /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout"/> per <see cref="Servers"/>.</remarks>
    /// <param name="query">The query.</param>
    /// <param name="predicate">A function to test each element for a condition.</param>
    /// <returns>The complete response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
    /// <exception cref="AggregateException">Could not reach any dns server.</exception>
    public DnsResponse ResolveSequential(DnsQuery query, Func<DnsResponse, bool> predicate)
    {
        if (query.Name is null)
        {
            throw new ArgumentException("Name must be provided");
        }
        if (query.Name.IsLocalhost) throw new InvalidOperationException("Do not lookup localhost via dns!");

        var exceptions = new List<Exception>();
        foreach (var server in Servers)
        {
            try
            {
                var response = QuerySingleServer(query, server, UseTcp);
                if (predicate(response))
                {
                    return response;
                }
                exceptions.Add(new($"Response {response.ResponseCode} {response.Answers?.FirstOrDefault()} not accepted!"));
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }

        throw new AggregateException("Could not resolve query!", exceptions);
    }

    /// <summary>Queries the dns servers for the specified records using all <see cref="SearchSuffixes"/>.</summary>
    /// <remarks>This method works parallel and returns the first result of any <see cref="Servers"/>.</remarks>
    /// <param name="domainName">Domain, that should be queried.</param>
    /// <param name="recordType">Type the should be queried.</param>
    /// <param name="recordClass">Class the should be queried.</param>
    /// <param name="flags">Options for the query.</param>
    /// <returns>The first successful response of the dns server.</returns>
    /// <exception cref="ArgumentNullException">Name must be provided.</exception>
    /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
    /// <exception cref="AggregateException">Could not reach any dns server.</exception>
    public DnsResponse ResolveWithSearchSuffix(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
    {
        if (domainName.IsLocalhost) throw new InvalidOperationException("Do not lookup localhost via dns!");
        SearchSuffixes ??= NoSuffix.Concat(NetworkInterface.GetAllNetworkInterfaces().Select(i => i.GetIPProperties().DnsSuffix)).Where(s => !string.IsNullOrEmpty(s)).Distinct().ToArray();
        var exceptions = new Exception[SearchSuffixes.Length];
        var responses = new DnsResponse[SearchSuffixes.Length];

        void Query(object? state)
        {
            if (state is not int n) return;
            try
            {
                responses[n] = Resolve(new()
                {
                    Name = $"{domainName}.{SearchSuffixes[n]}".Trim('.'),
                    RecordType = recordType,
                    RecordClass = recordClass,
                    Flags = flags
                });
            }
            catch (Exception ex)
            {
                exceptions[n] = ex;
            }
        }

        var tasks = new Task[SearchSuffixes.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            object parameter = i;
            tasks[i] = Task.Factory.StartNew(Query, parameter);
        }

        while (tasks.Length > 0)
        {
            Task.WaitAny(tasks);
            var response = responses.FirstOrDefault(r => r?.ResponseCode == DnsResponseCode.NoError);
            if (response != null)
            {
                return response;
            }
            tasks = tasks.Where(t => !t.IsCompleted).ToArray();
        }
        return SelectBestResponse(responses, exceptions);
    }

    #endregion Public Methods
}
