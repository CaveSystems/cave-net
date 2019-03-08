using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Cave.Net
{
    /// <summary>
    /// Network specific settings and addresses.
    /// </summary>
    public static class NetTools
    {
        static string myHostName = null;

        /// <summary>Determines whether the specified address is localhost.</summary>
        /// <param name="address">The address.</param>
        /// <returns><c>true</c> if the specified address is localhost; otherwise, <c>false</c>.</returns>
        public static bool IsLocalhost(this IPAddress address)
        {
            string stringAddress = address.ToString();
            switch (address.AddressFamily)
            {
                case AddressFamily.InterNetwork: return stringAddress.StartsWith("127.");
                case AddressFamily.InterNetworkV6: return stringAddress.StartsWith("::ffff:127.") || stringAddress == "::1";
            }
            return false;
        }

        /// <summary>
        /// Parses a string for a valid IPAddress[:Port] or DnsName[:Port] combination and retrieves all
        /// matching <see cref="IPEndPoint"/>s. If no port is specified DefaultPort will be returned.
        /// </summary>
        /// <param name="text">The string containing the ip endpoint (server[:port] or ipaddress[:port]).</param>
        /// <param name="defaultPort">The default port used if no port was given.</param>
        public static IPEndPoint[] GetIPEndPoints(string text, int defaultPort)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            int port = defaultPort;
            int portIndex = text.LastIndexOf(':');
            if (portIndex > -1)
            {
                string portString = text.Substring(portIndex + 1);
                if (int.TryParse(portString, out port))
                {
                    text = text.Substring(0, portIndex);
                }
            }

            if (text == string.Empty || text == "any")
            {
                return new IPEndPoint[]
                {
                    new IPEndPoint(IPAddress.Any, port),
                    new IPEndPoint(IPAddress.IPv6Any, port),
                };
            }

            if (IPAddress.TryParse(text, out IPAddress a))
            {
                return new IPEndPoint[] { new IPEndPoint(a, port) };
            }

            var result = new List<IPEndPoint>();
            foreach (IPAddress address in System.Net.Dns.GetHostEntry(text).AddressList)
            {
                result.Add(new IPEndPoint(address, port));
            }
            return result.ToArray();
        }

        /// <summary>
        /// Retrieves all local addresses.
        /// </summary>
        public static UnicastIPAddressInformation[] GetLocalAddresses()
        {
            var result = new List<UnicastIPAddressInformation>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                IPInterfaceProperties p = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation ip in p.UnicastAddresses)
                {
                    result.Add(ip);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Retrieves local addresses with the specified addresfamily.
        /// </summary>
        /// <returns></returns>
        public static IPAddress[] GetLocalAddresses(AddressFamily addressFamily)
        {
            var result = new List<IPAddress>();
            foreach (IPAddress addr in System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()))
            {
                if (addr.AddressFamily == addressFamily)
                {
                    result.Add(addr);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Obtains the first available <see cref="IPAddress"/> for the specified hostname with the
        /// specified <see cref="AddressFamily"/>.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="addressFamily"></param>
        /// <returns></returns>
        public static IPAddress GetAddress(string hostName, AddressFamily addressFamily)
        {
            foreach (IPAddress address in System.Net.Dns.GetHostAddresses(hostName))
            {
                if (address.AddressFamily == addressFamily)
                {
                    return address;
                }
            }
            throw new ArgumentException(string.Format("Could not find an IPAddress for {0} with AddressFamily {1}!", hostName, addressFamily));
        }

        /// <summary>
        /// Obtains all available <see cref="IPAddress"/> for the specified hostname with the
        /// specified <see cref="AddressFamily"/>.
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="addressFamily"></param>
        /// <returns></returns>
        public static IPAddress[] GetAddresses(string hostName, AddressFamily addressFamily)
        {
            var addresses = new List<IPAddress>();
            foreach (IPAddress address in System.Net.Dns.GetHostAddresses(hostName))
            {
                if (address.AddressFamily == addressFamily)
                {
                    addresses.Add(address);
                }
            }
            return addresses.ToArray();
        }

        /// <summary>Tries to find a free tcp port.</summary>
        /// <param name="defaultPort">The default port.</param>
        /// <param name="maximumTries">The maximum tries.</param>
        /// <returns></returns>
        public static ushort GetFreeTcpPort(int defaultPort, int maximumTries = 10)
        {
            int port = defaultPort;
            try
            {
#if NET45 || NET46 || NET47 || NETSTANDARD20
                TcpListener listener = TcpListener.Create(port);
#elif NET20 || NET35 || NET40
#pragma warning disable CS0618
                var listener = new TcpListener(IPAddress.Any, port);
#pragma warning restore CS0618
#else
#error No code defined for the current framework or NETXX version define missing!
#endif
                listener.Start();
                listener.Stop();
                return (ushort)port;
            }
            catch
            {
            }
            for (int i = 0; ; i++)
            {
                try
                {
                    var listener = new TcpListener(IPAddress.Any, 0);
                    listener.Start();
                    port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    listener.Stop();
                    return (ushort)port;
                }
                catch
                {
                    if (i >= maximumTries)
                    {
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Obtains the fqdn (hostname) of the system.
        /// </summary>
        static string GetHostName()
        {
            string hostName = null;
            {
                try
                {
                    hostName = IPGlobalProperties.GetIPGlobalProperties().HostName;
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try
                {
                    hostName = System.Net.Dns.GetHostName();
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try
                {
                    hostName = Environment.MachineName;
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try
                {
                    hostName = Environment.GetEnvironmentVariable("COMPUTERNAME");
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try
                {
                    hostName = Environment.GetEnvironmentVariable("HOSTNAME");
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                hostName = "localhost";
            }

            string domainName = null;
            {
                try
                {
                    domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(domainName))
            {
                try
                {
                    domainName = Environment.UserDomainName;
                }
                catch
                {
                }
            }
            if (string.IsNullOrEmpty(domainName))
            {
                domainName = "localdomain";
            }

            string[] names = new string[]
                {
                    hostName + "." + domainName,
                    hostName,
                };

            foreach (string str in names)
            {
                try
                {
                    IPHostEntry entry = System.Net.Dns.GetHostEntry(str);
                    myHostName = entry.HostName;
                    return myHostName;
                }
                catch
                {
                }
            }

            // no network fallback
            return hostName + "." + domainName;
        }

        /// <summary>
        /// Gets the fqdn (hostname) caching the result.
        /// </summary>
        public static string HostName
        {
            get
            {
                if (myHostName != null)
                {
                    return myHostName;
                }

                myHostName = GetHostName();
                return myHostName;
            }
        }
    }
}
