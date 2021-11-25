using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>
    /// Provides a client for querying dns records.
    /// </summary>
    public class DnsClient
    {
        #region static class

        static void LoadEtcResolvConf(List<IPAddress> result)
        {
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
                            s = s.Substring(0, i);
                        }

                        var parts = s.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 1)
                        {
                            if (parts[0].ToUpper() != "NAMESERVER")
                            {
                                continue;
                            }

                            for (var n = 1; n < parts.Length; n++)
                            {
                                if (IPAddress.TryParse(parts[n], out var addr))
                                {
                                    result.Add(addr);
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

        /// <summary>
        /// Gets a default instance of the DnsClient, which uses the configured dns servers of the executing computer and a query timeout of 10 seconds.
        /// </summary>
        public static DnsClient Default { get; } = new() { Servers = GetDefaultDnsServers() };

        /// <summary>
        /// Gets a default instance of the DnsClient, which uses the configured dns servers of the executing computer and a query timeout of 10 seconds.
        /// </summary>
        public static DnsClient Google
        {
            get
            {
                var google = new DnsClient()
                {
                    Servers = new[]
                    {
                        IPAddress.Parse("8.8.4.4"),
                        IPAddress.Parse("8.8.8.8"),
                        IPAddress.Parse("2001:4860:4860::8844"),
                        IPAddress.Parse("2001:4860:4860::8888"),
                    }
                };
                return google;
            }
        }

        /// <summary>
        /// Gets a list of the local configured DNS servers.
        /// </summary>
        /// <returns>Returns a array of <see cref="IPAddress"/> instances.</returns>
        public static IPAddress[] GetDefaultDnsServers()
        {
            var result = new List<IPAddress>();
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if ((nic.OperationalStatus == OperationalStatus.Up) && (nic.NetworkInterfaceType != NetworkInterfaceType.Loopback))
                    {
                        result.AddRange(nic.GetIPProperties().DnsAddresses);
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

                default:
                    LoadEtcResolvConf(result);
                    break;
            }

            if (result.Count == 0)
            {
                Trace.TraceWarning("Cannot use the default DNS servers of this system. Using public dns.");
                return GetPulicDnsServers();
            }

            return result.ToArray();
        }

        /// <summary>
        /// Gets a list of public DNS servers (EU and US).
        /// </summary>
        public static IPAddress[] GetPulicDnsServers()
        {
            var result = new List<IPAddress>();
            //Deutsche Telekom AG
            result.Add(IPAddress.Parse("194.25.0.60"));
            //uunet germany
            result.Add(IPAddress.Parse("193.101.111.10"));
            //uunet france
            result.Add(IPAddress.Parse("194.98.65.65"));
            //cloudflare usa
            result.Add(IPAddress.Parse("1.1.1.1"));
            result.Add(IPAddress.Parse("2606:4700:4700::1001"));
            //google
            result.Add(IPAddress.Parse("8.8.4.4"));
            result.Add(IPAddress.Parse("8.8.8.8"));
            result.Add(IPAddress.Parse("2001:4860:4860::8844"));
            result.Add(IPAddress.Parse("2001:4860:4860::8888"));
            return result.ToArray();
        }

        #endregion static class

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DnsClient"/> class.
        /// </summary>
        public DnsClient()
        {
            UseUdp = true;
            UseTcp = true;
            Port = 53;
            QueryTimeout = TimeSpan.FromSeconds(5);
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        public ushort Port { get; set; }

        /// <summary>
        /// Gets or sets the query timeout.
        /// </summary>
        /// <value>The query timeout.</value>
        public TimeSpan QueryTimeout { get; set; }

        /// <summary>
        /// Gets or sets the search suffixes for short names.
        /// </summary>
        /// <value>The dns suffixes.</value>
        public string[] SearchSuffixes { get; set; }

        /// <summary>
        /// Gets or sets the servers.
        /// </summary>
        /// <value>The servers.</value>
        public IPAddress[] Servers { get; set; }

        /// <summary>
        /// Gets a value indicating whether [use random case].
        /// </summary>
        /// <value><c>true</c> if [use random case]; otherwise, <c>false</c>.</value>
        /// <remarks><see href="https://tools.ietf.org/html/draft-vixie-dnsext-dns0x20-00"/>.</remarks>
        public bool UseRandomCase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether queries can be sent using TCP.
        /// </summary>
        public bool UseTcp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether queries can be sent using UDP.
        /// </summary>
        public bool UseUdp { get; set; }

        #endregion Properties

        #region Members

        static DnsResponse SelectBestResponse(IEnumerable<DnsResponse> responses, IEnumerable<Exception> errors)
        {
            var answers = responses.Where(r => r != null).ToList();
            return
                answers.FirstOrDefault(r => r?.ResponseCode == DnsResponseCode.NoError) ??
                answers.FirstOrDefault(r => r?.Answers.Count > 0) ??
                answers.FirstOrDefault() ??
                throw new AggregateException("Could not reach any dns server!", errors.Where(e => e is not null));
        }

        bool QueryAllServers(DnsQuery query, Func<DnsResponse, bool> completed, out IList<DnsResponse> responses, out IList<Exception> exceptions)
        {
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
                    var response = QuerySingleServer(query, server, useTcp);
                    if (response == null) throw new();
                    lock (results) results[resultNumber++] = response;
                    if (completed != null)
                    {
                        done |= completed(response);
                    }
                }
                catch (Exception ex)
                {
                    lock (results) results[resultNumber++] = ex;
                }
            }

            foreach (var server in Servers)
            {
                if (!skipUdp && UseUdp)
                {
                    tasks.Add(Task.Factory.StartNew(() => Query(server, false)), $"{query} @udp://{server}");
                }
                if (UseTcp)
                {
                    tasks.Add(Task.Factory.StartNew(() => Query(server, true)), $"{query} @tcp://{server}");
                }
            }
            if (tasks.Count == 0) throw new Exception("Query could not be sent to any servers. Check UseTcp, Servers and Query.Length!");

            while (!done)
            {
                var tasksRunning = tasks.Keys.Where(t => !t.IsCompleted).ToArray();
                if (tasksRunning.Length == 0) break;
                Task.WaitAny(tasksRunning);
            }
            lock (results)
            {
                exceptions = results.Select(r => r is Exception ex ? ex : null).Where(r => r is not null).Cast<Exception>().ToList();
                responses = results.Select(r => r is DnsResponse d ? d : null).Where(r => r is not null).Cast<DnsResponse>().ToList();
                if (exceptions.Count == 0 && responses.Count == 0) throw new();
            }
            return done;
        }

        DnsResponse QuerySingleServer(DnsQuery query, IPAddress server, bool useTcp)
        {
            var messageID = DefaultRNG.UInt16;
            var message = query.ToArray(messageID);
            var response = useTcp ? QueryTcp(server, message) : QueryUdp(server, message);
            return response.TransactionID != messageID
                ? throw new InvalidDataException("Invalid message ID received!")
                : (response.Queries.Count != 1 || response.Queries[0] != query)
                ? throw new InvalidDataException("Invalid answer received!")
                : response;
        }

        DnsResponse QueryTcp(IPAddress srv, byte[] query)
        {
            var timeout = Math.Max(100, (int)QueryTimeout.TotalMilliseconds);
            TcpAsyncClient tcp = null;
            try
            {
                tcp = new();
                tcp.ConnectTimeout = timeout;
                tcp.Connect(srv, 53);
                tcp.SendTimeout = timeout;
                tcp.ReceiveTimeout = timeout;
                tcp.Stream.DirectWrites = true;
                var writer = new DataWriter(tcp.Stream, endian: EndianType.BigEndian);
                writer.Write((ushort)query.Length);
                writer.Write(query);
                writer.Flush();
                var reader = new DataReader(tcp.Stream, endian: EndianType.BigEndian);
                var length = reader.ReadUInt16();
                var data = reader.ReadBytes(length);
                return new DnsResponse(srv, data);
            }
            finally
            {
                tcp?.Close();
            }
        }

        DnsResponse QueryUdp(IPAddress srv, byte[] query)
        {
            if (query.Length > 512) throw new ArgumentException("Udp query may not exceed 512 bytes!");
            var timeout = Math.Max(100, (int)QueryTimeout.TotalMilliseconds);
            UdpClient udp = null;
            try
            {
                udp = new UdpClient(srv.AddressFamily);
                udp.Connect(srv, 53);
                udp.Client.SendTimeout = timeout;
                udp.Client.ReceiveTimeout = timeout;
                udp.Send(query, query.Length);
                var remote = new IPEndPoint(IPAddress.Any, 0);
                var data = udp.Receive(ref remote);
                var response = new DnsResponse(srv, data);
                return response;
            }
            finally
            {
                udp?.Close();
            }
        }

        /// <summary>
        /// Queries the dns servers for the specified records.
        /// </summary>
        /// <remarks>This method works parallel and returns the first result of any <see cref="Servers"/>.</remarks>
        /// <param name="domainName">Domain, that should be queried.</param>
        /// <param name="recordType">Type the should be queried.</param>
        /// <param name="recordClass">Class the should be queried.</param>
        /// <param name="flags">Options for the query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        public DnsResponse Resolve(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
        {
            if (Servers == null)
            {
                Servers = GetDefaultDnsServers();
            }

            if (domainName.Parts.Length == 1)
            {
                return ResolveWithSearchSuffix(domainName, recordType, recordClass, flags);
            }

            return Resolve(new DnsQuery
            {
                Name = domainName,
                RecordType = recordType,
                RecordClass = recordClass,
                Flags = flags
            });
        }

        /// <summary>
        /// Queries the dns servers for the specified records.
        /// </summary>
        /// <remarks>This method works parallel and returns the first result of any <see cref="Servers"/>.</remarks>
        /// <param name="query">The query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
        /// <exception cref="AggregateException">Could not reach any dns server.</exception>
        public DnsResponse Resolve(DnsQuery query)
        {
            if (Servers == null)
            {
                Servers = GetDefaultDnsServers();
            }

            if (query.Name == null)
            {
                throw new ArgumentNullException(nameof(query.Name), "Name must be provided");
            }

            var skipUdp = query.Length > 512;
            if (skipUdp)
            {
                if (!UseTcp)
                {
                    throw new Exception("Query to big for UDP transmission. Enable UseTcp!");
                }
            }

            if (QueryAllServers(query, r => r.ResponseCode == DnsResponseCode.NoError, out var responses, out var exceptions))
            {
                return SelectBestResponse(responses, exceptions);
            }
            throw new AggregateException("Could not complete query.", exceptions);
        }

        /// <summary>
        /// Queries the dns servers for the specified records.
        /// </summary>
        /// <remarks>This method works parallel and returns all results received from all <see cref="Servers"/>.</remarks>
        /// <param name="domainName">Domain, that should be queried.</param>
        /// <param name="recordType">Type the should be queried.</param>
        /// <param name="recordClass">Class the should be queried.</param>
        /// <param name="flags">Options for the query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        public IList<DnsResponse> ResolveAll(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired) => ResolveAll(new DnsQuery()
        {
            Name = domainName,
            RecordType = recordType,
            RecordClass = recordClass,
            Flags = flags,
        });

        /// <summary>
        /// Queries the dns servers for the specified records.
        /// </summary>
        /// <remarks>This method works parallel and returns all results received from all <see cref="Servers"/>.</remarks>
        /// <param name="query">The query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
        /// <exception cref="AggregateException">Could not reach any dns server.</exception>
        public IList<DnsResponse> ResolveAll(DnsQuery query)
        {
            if (Servers == null)
            {
                Servers = GetDefaultDnsServers();
            }

            if (query.Name == null)
            {
                throw new ArgumentNullException(nameof(query.Name), "Name must be provided");
            }

            var skipUdp = query.Length > 512;
            if (skipUdp)
            {
                if (!UseTcp)
                {
                    throw new Exception("Query to big for UDP transmission. Enable UseTcp!");
                }
            }

            if (QueryAllServers(query, null, out var responses, out var exceptions) || responses.Any())
            {
                return responses;
            }
            throw new AggregateException("Could not complete query.", exceptions);
        }

        public DnsResponse ResolveSequential(IPAddress address)
             => ResolveSequential(address.GetReverseLookupZone(), DnsRecordType.PTR, DnsRecordClass.IN, DnsFlags.RecursionDesired);
        
        /// <summary>
        /// Queries the dns servers for the specified records.
        /// </summary>
        /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout"/> per <see cref="Servers"/>.</remarks>
        /// <param name="domainName">Domain, that should be queried.</param>
        /// <param name="recordType">Type the should be queried.</param>
        /// <param name="recordClass">Class the should be queried.</param>
        /// <param name="flags">Options for the query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        public DnsResponse ResolveSequential(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired) => ResolveSequential(new DnsQuery()
        {
            Name = domainName,
            RecordType = recordType,
            RecordClass = recordClass,
            Flags = flags,
        });

        /// <summary>
        /// Queries the dns servers for the specified records.
        /// </summary>
        /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout"/> per <see cref="Servers"/>.</remarks>
        /// <param name="query">The query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
        /// <exception cref="AggregateException">Could not reach any dns server.</exception>
        public DnsResponse ResolveSequential(DnsQuery query) => ResolveSequential(query, (r) => r.ResponseCode == DnsResponseCode.NoError);

        /// <summary>
        /// Queries the dns servers for the specified records.
        /// </summary>
        /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout"/> per <see cref="Servers"/>.</remarks>
        /// <param name="query">The query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
        /// <exception cref="AggregateException">Could not reach any dns server.</exception>
        public DnsResponse ResolveSequential(DnsQuery query, Func<DnsResponse, bool> predicate)
        {
            if (Servers == null)
            {
                Servers = GetDefaultDnsServers();
            }

            if (query.Name == null)
            {
                throw new ArgumentNullException(nameof(query.Name), "Name must be provided");
            }

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

        /// <summary>
        /// Queries the dns servers for the specified records using all <see cref="SearchSuffixes"/>.
        /// </summary>
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
            if (SearchSuffixes == null)
            {
                SearchSuffixes = NetworkInterface.GetAllNetworkInterfaces().Select(i => i.GetIPProperties().DnsSuffix).Distinct().ToArray();
            }

            var exceptions = new Exception[SearchSuffixes.Length];
            var responses = new DnsResponse[SearchSuffixes.Length];

            var tasks = new Task[SearchSuffixes.Length];
            for (var i = 0; i < SearchSuffixes.Length; i++)
            {
                void Query(object state)
                {
                    var n = (int)state;
                    try
                    {
                        responses[n] = Resolve(new DnsQuery
                        {
                            Name = $"{domainName}.{SearchSuffixes[n]}",
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
                tasks[i] = Task.Factory.StartNew(Query, (object)i);
            };

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

        #endregion Members
    }
}
