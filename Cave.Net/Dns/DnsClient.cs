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
    /// <summary>Provides a client for querying dns records.</summary>
    public class DnsClient
    {
        #region static class

        static DnsClient defaultClient;

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
        /// Gets a default instance of the DnsClient, which uses the configured dns servers of the executing computer and a query timeout of
        /// 10 seconds.
        /// </summary>
        public static DnsClient Default
        {
            get
            {
                if (defaultClient == null)
                {
                    defaultClient = new DnsClient
                    {
                        Servers = GetDefaultDnsServers()
                    };
                }

                return defaultClient;
            }
        }

        /// <summary>
        /// Gets a default instance of the DnsClient, which uses the configured dns servers of the executing computer and a query timeout of
        /// 10 seconds.
        /// </summary>
        public static DnsClient Google
        {
            get
            {
                var google = defaultClient = new DnsClient()
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

        /// <summary>Gets a list of the local configured DNS servers.</summary>
        /// <returns>Returns a array of <see cref="IPAddress" /> instances.</returns>
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

            // public dns as fallback
            {
                if (result.Count == 0)
                {
                    Trace.TraceWarning("Cannot use the default DNS servers of this system. Using public dns.");
                }
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
            }
            return result.ToArray();
        }

        #endregion

        #region Constructors

        /// <summary>Initializes a new instance of the <see cref="DnsClient" /> class.</summary>
        public DnsClient()
        {
            UseUdp = true;
            UseTcp = true;
            Port = 53;
            QueryTimeout = TimeSpan.FromSeconds(5);
        }

        #endregion

        #region Properties

        /// <summary>Gets a value indicating whether [use random case].</summary>
        /// <value><c>true</c> if [use random case]; otherwise, <c>false</c>.</value>
        /// <remarks><see href="https://tools.ietf.org/html/draft-vixie-dnsext-dns0x20-00" />.</remarks>
        public bool UseRandomCase { get; set; }

        /// <summary>Gets or sets the port.</summary>
        /// <value>The port.</value>
        public ushort Port { get; set; }

        /// <summary>Gets or sets the query timeout.</summary>
        /// <value>The query timeout.</value>
        public TimeSpan QueryTimeout { get; set; }

        /// <summary>Gets or sets the search suffixes for short names.</summary>
        /// <value>The dns suffixes.</value>
        public string[] SearchSuffixes { get; set; }

        /// <summary>Gets or sets the servers.</summary>
        /// <value>The servers.</value>
        public IPAddress[] Servers { get; set; }

        /// <summary>Gets or sets a value indicating whether queries can be sent using TCP.</summary>
        public bool UseTcp { get; set; }

        /// <summary>Gets or sets a value indicating whether queries can be sent using UDP.</summary>
        public bool UseUdp { get; set; }

        #endregion

        #region Members

        /// <summary>Queries the dns servers for the specified records.</summary>
        /// <remarks>This method works parallel and returns the first result of any <see cref="Servers" />.</remarks>
        /// <param name="domainName">Domain, that should be queried.</param>
        /// <param name="recordType">Type the should be queried.</param>
        /// <param name="recordClass">Class the should be queried.</param>
        /// <param name="flags">Options for the query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        public DnsResponse Resolve(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN,
            DnsFlags flags = DnsFlags.RecursionDesired)
        {
            if ((domainName.Parts.Length == 1) && (SearchSuffixes?.Length > 0))
            {
                var responses = new List<DnsResponse>();
                Parallel.ForEach(SearchSuffixes, suffix =>
                {
                    var answer = Resolve($"{domainName}.{suffix}", recordType, recordClass, flags);
                    lock (responses)
                    {
                        responses.Add(answer);
                    }
                });
                return SelectBestResponse(responses, null);
            }

            return Resolve(new DnsQuery
            {
                Name = domainName,
                RecordType = recordType,
                RecordClass = recordClass,
                Flags = flags
            });
        }

        /// <summary>Queries the dns servers for the specified records.</summary>
        /// <remarks>This method works parallel and returns the first result of any <see cref="Servers" />.</remarks>
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

            var messageID = DefaultRNG.UInt16;

            // question = name, recordtype, recordclass
            byte[] message;
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream, StringEncoding.ASCII, endian: EndianType.BigEndian);
                writer.Write(messageID); // transaction id
                writer.Write((ushort)query.Flags); // flags
                writer.Write((ushort)1); // question records
                writer.Write((ushort)0); // answer records
                writer.Write((ushort)0); // authority records
                writer.Write((ushort)0); // additional records
                if (UseRandomCase)
                {
                    query.RandomizeCase();
                }

                query.Write(writer);
                message = stream.ToArray();
            }

            var useTcp = false;
            if (message.Length > 512)
            {
                if (!UseTcp)
                {
                    throw new Exception("Query to big for UDP transmission. Enable UseTcp!");
                }

                useTcp = true;
            }

            var exceptions = new Exception[Servers.Length];
            var responses = new DnsResponse[Servers.Length];

            var tasks = new Task[Servers.Length];
            for (var i = 0; i < Servers.Length; i++)
            {
                void Query(object state)
                {
                    var n = (int)state;
                    try
                    {
                        var response = DoQuery(useTcp, query, Servers[n], messageID, message);
                        if (response != null)
                        {
                            responses[n] = response;
                        }
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

        /// <summary>Queries the dns servers for the specified records.</summary>
        /// <remarks>This method works parallel and returns all results received from all <see cref="Servers" />.</remarks>
        /// <param name="domainName">Domain, that should be queried.</param>
        /// <param name="recordType">Type the should be queried.</param>
        /// <param name="recordClass">Class the should be queried.</param>
        /// <param name="flags">Options for the query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        public IList<DnsResponse> ResolveAll(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
        {
            return ResolveAll(new DnsQuery()
            {
                Name = domainName,
                RecordType = recordType,
                RecordClass = recordClass,
                Flags = flags,
            });
        }

        /// <summary>Queries the dns servers for the specified records.</summary>
        /// <remarks>This method works parallel and returns all results received from all <see cref="Servers" />.</remarks>
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

            var messageID = DefaultRNG.UInt16;

            // question = name, recordtype, recordclass
            byte[] message;
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream, StringEncoding.ASCII, endian: EndianType.BigEndian);
                writer.Write(messageID); // transaction id
                writer.Write((ushort)query.Flags); // flags
                writer.Write((ushort)1); // question records
                writer.Write((ushort)0); // answer records
                writer.Write((ushort)0); // authority records
                writer.Write((ushort)0); // additional records
                if (UseRandomCase)
                {
                    query.RandomizeCase();
                }

                query.Write(writer);
                message = stream.ToArray();
            }

            var useTcp = false;
            if (message.Length > 512)
            {
                if (!UseTcp)
                {
                    throw new Exception("Query to big for UDP transmission. Enable UseTcp!");
                }

                useTcp = true;
            }

            var result = new List<DnsResponse>();
            Parallel.ForEach(Servers, (server, state) =>
            {
                try
                {
                    var response = DoQuery(useTcp, query, server, messageID, message);
                    if (response != null)
                    {
                        lock (result)
                        {
                            result.Add(response);
                        }
                    }
                }
                catch { }
            });
            return result;
        }

        /// <summary>Queries the dns servers for the specified records.</summary>
        /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout" /> per <see cref="Servers" />.</remarks>
        /// <param name="domainName">Domain, that should be queried.</param>
        /// <param name="recordType">Type the should be queried.</param>
        /// <param name="recordClass">Class the should be queried.</param>
        /// <param name="flags">Options for the query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        public DnsResponse ResolveSequential(DomainName domainName, DnsRecordType recordType = DnsRecordType.A, DnsRecordClass recordClass = DnsRecordClass.IN, DnsFlags flags = DnsFlags.RecursionDesired)
        {
            return ResolveSequential(new DnsQuery()
            {
                Name = domainName,
                RecordType = recordType,
                RecordClass = recordClass,
                Flags = flags,
            });
        }

        /// <summary>Queries the dns servers for the specified records.</summary>
        /// <remarks>This method works sequential and may need up to <see cref="QueryTimeout" /> per <see cref="Servers" />.</remarks>
        /// <param name="query">The query.</param>
        /// <returns>The complete response of the dns server.</returns>
        /// <exception cref="ArgumentNullException">Name must be provided.</exception>
        /// <exception cref="Exception">Query to big for UDP transmission. Enable UseTcp.</exception>
        /// <exception cref="AggregateException">Could not reach any dns server.</exception>
        public DnsResponse ResolveSequential(DnsQuery query)
        {
            if (Servers == null)
            {
                Servers = GetDefaultDnsServers();
            }

            if (query.Name == null)
            {
                throw new ArgumentNullException(nameof(query.Name), "Name must be provided");
            }

            var messageID = DefaultRNG.UInt16;

            // question = name, recordtype, recordclass
            byte[] message;
            using (var stream = new MemoryStream())
            {
                var writer = new DataWriter(stream, StringEncoding.ASCII, endian: EndianType.BigEndian);
                writer.Write(messageID); // transaction id
                writer.Write((ushort)query.Flags); // flags
                writer.Write((ushort)1); // question records
                writer.Write((ushort)0); // answer records
                writer.Write((ushort)0); // authority records
                writer.Write((ushort)0); // additional records
                if (UseRandomCase)
                {
                    query.RandomizeCase();
                }

                query.Write(writer);
                message = stream.ToArray();
            }

            var useTcp = false;
            if (message.Length > 512)
            {
                if (!UseTcp)
                {
                    throw new Exception("Query to big for UDP transmission. Enable UseTcp!");
                }

                useTcp = true;
            }

            var exceptions = new List<Exception>();
            DnsResponse response = null;
            foreach (var server in Servers)
            {
                try
                {
                    response = DoQuery(useTcp, query, server, messageID, message);
                    if (response != null)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }

            return response ?? throw new AggregateException("Could not reach any dns server!", exceptions);
        }

        DnsResponse DoQuery(bool useTcp, DnsQuery query, IPAddress server, ushort messageID, byte[] message)
        {
            while (true)
            {
                DnsResponse response;
                try
                {
                    response = useTcp ? QueryTcp(server, message) : QueryUdp(server, message);
                    if (response.IsTruncatedResponse && !useTcp)
                    {
                        useTcp = true;
                        continue;
                    }
                }
                catch
                {
                    if (useTcp)
                    {
                        throw;
                    }

                    useTcp = true;
                    continue;
                }

                return response.TransactionID != messageID
                    ? throw new InvalidDataException("Invalid message ID received!")
                    : response.Queries.Count != 1
                        ? throw new InvalidDataException("Invalid answer received!")
                        : response.Queries[0] != query
                            ? throw new InvalidDataException("Invalid answer received!")
                            : response.IsTruncatedResponse
                                ? throw new InvalidDataException("Truncated answer received!")
                                : response;
            }
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

        DnsResponse SelectBestResponse(IEnumerable<DnsResponse> responses, IEnumerable<Exception> errors)
        {
            var answers = responses.Where(r => r != null).ToList();
            return
                answers.FirstOrDefault(r => r?.ResponseCode == DnsResponseCode.NoError) ??
                answers.FirstOrDefault(r => r?.Answers.Count > 0) ??
                answers.FirstOrDefault() ??
                throw new AggregateException("Could not reach any dns server!", errors.Where(e => e is not null));
        }

        #endregion
    }
}
