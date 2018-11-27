#region CopyRight 2018
/*
    Copyright (c) 2007-2018 Andreas Rohleder (andreas@rohleder.cc)
    All rights reserved
*/
#endregion
#region License LGPL-3
/*
    This program/library/sourcecode is free software; you can redistribute it
    and/or modify it under the terms of the GNU Lesser General Public License
    version 3 as published by the Free Software Foundation subsequent called
    the License.

    You may not use this program/library/sourcecode except in compliance
    with the License. The License is included in the LICENSE file
    found at the installation directory or the distribution package.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be included
    in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion

using Cave.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Cave.Net
{
    /// <summary>
    /// Network specific settings and addresses
    /// </summary>
    public static class NetTools
    {
        static string s_HostName = null;

		/// <summary>Determines whether the specified address is localhost.</summary>
		/// <param name="address">The address.</param>
		/// <returns><c>true</c> if the specified address is localhost; otherwise, <c>false</c>.</returns>
		public static bool IsLocalhost(IPAddress address)
		{
			var stringAddress = address.ToString();
			switch (address.AddressFamily)
			{
				case AddressFamily.InterNetwork: return (stringAddress.StartsWith("127."));
				case AddressFamily.InterNetworkV6: return stringAddress.StartsWith("::ffff:127.") || stringAddress == "::1";
			}
			return false;
		}

		/// <summary>
		/// Parses a string for a valid IPAddress[:Port] or DnsName[:Port] combination and retrieves all 
		/// matching <see cref="IPEndPoint"/>s. If no port is specified DefaultPort will be returned.
		/// </summary>
		/// <param name="text">The string containing the ip endpoint (server[:port] or ipaddress[:port])</param>
		/// <param name="defaultPort">The default port used if no port was given</param>
		public static IPEndPoint[] GetIPEndPoints(string text, int defaultPort)
        {
            if (string.IsNullOrEmpty(text)) return null;

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

            if (text == "" || text == "any")
            {
                return new IPEndPoint[]
                {
                    new IPEndPoint(IPAddress.Any, port),
                    new IPEndPoint(IPAddress.IPv6Any, port),
                };
            }

            IPAddress a;
            if (IPAddress.TryParse(text, out a))
            {
                return new IPEndPoint[] { new IPEndPoint(a, port) };
            }

            List<IPEndPoint> result = new List<IPEndPoint>();
            foreach (IPAddress address in Dns.GetHostEntry(text).AddressList)
            {
                result.Add(new IPEndPoint(address, port));
            }
            return result.ToArray();
        }

        /// <summary>
        /// Retrieves all local addresses
        /// </summary>
        public static UnicastIPAddressInformation[] GetLocalAddresses()
        {
			Set<UnicastIPAddressInformation> result = new Set<UnicastIPAddressInformation>();
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                var p = ni.GetIPProperties();
                foreach (var ip in p.UnicastAddresses)
                {
					result.Include(ip);
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// Retrieves local addresses with the specified addresfamily
        /// </summary>
        public static IPAddress[] GetLocalAddresses(AddressFamily addressFamily)
        {
            List<IPAddress> result = new List<IPAddress>();
            foreach (IPAddress addr in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (addr.AddressFamily == addressFamily) result.Add(addr);
            }
            return result.ToArray();
        }

        /// <summary>
        /// Obtains the first available <see cref="IPAddress"/> for the specified hostname with the
        /// specified <see cref="AddressFamily"/>
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="addressFamily"></param>
        /// <returns></returns>
        public static IPAddress GetAddress(string hostName, AddressFamily addressFamily)
        {
            foreach (IPAddress address in Dns.GetHostAddresses(hostName))
            {
                if (address.AddressFamily == addressFamily) return address;
            }
            throw new ArgumentException(string.Format("Could not find an IPAddress for {0} with AddressFamily {1}!", hostName, addressFamily));
        }

        /// <summary>
        /// Obtains all available <see cref="IPAddress"/> for the specified hostname with the
        /// specified <see cref="AddressFamily"/>
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="addressFamily"></param>
        /// <returns></returns>
        public static IPAddress[] GetAddresses(string hostName, AddressFamily addressFamily)
        {
            List<IPAddress> addresses = new List<IPAddress>();
            foreach (IPAddress address in Dns.GetHostAddresses(hostName))
            {
                if (address.AddressFamily == addressFamily) addresses.Add(address);
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
#if NET45 || NET46 || NET471 || NETSTANDARD20
				TcpListener listener = TcpListener.Create(port);
#elif NET20 || NET35 || NET40
#pragma warning disable CS0618
				TcpListener listener = new TcpListener(IPAddress.Any, port);
#pragma warning restore CS0618
#else
#error No code defined for the current framework or NETXX version define missing!
#endif
                listener.Start();
                listener.Stop();
                return (ushort)port;
            }
            catch { }
            for (int i = 0; ; i++)
            {
                try
                {
                    TcpListener listener = new TcpListener(IPAddress.Any, 0);
                    listener.Start();
                    port = ((IPEndPoint)listener.LocalEndpoint).Port;
                    listener.Stop();
                    return (ushort)port;
                }
                catch
                {
                    if (i >= maximumTries) throw;
                }
            }
        }

        /// <summary>
        /// Obtains the fqdn (hostname) of the system
        /// </summary>
        static string GetHostName()
        {
            string hostName = null;
            {
                try { hostName = IPGlobalProperties.GetIPGlobalProperties().HostName; }
                catch { }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try { hostName = Dns.GetHostName(); }
                catch { }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try { hostName = Environment.MachineName; }
                catch { }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try { hostName = Environment.GetEnvironmentVariable("COMPUTERNAME"); }
                catch { }
            }
            if (string.IsNullOrEmpty(hostName))
            {
                try { hostName = Environment.GetEnvironmentVariable("HOSTNAME"); }
                catch { }
            }
            if (string.IsNullOrEmpty(hostName)) hostName = "localhost";

            string domainName = null;
            {
                try { domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName; }
                catch { }
            }
            if (string.IsNullOrEmpty(domainName))
            {
                try { domainName = Environment.UserDomainName; }
                catch { }
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
                    IPHostEntry entry = Dns.GetHostEntry(str);
                    s_HostName = entry.HostName;
                    return s_HostName;
                }
                catch { }
            }
            //no network fallback
            return hostName + "." + domainName;
        }

        /// <summary>
        /// Obtains the fqdn (hostname) caching the result
        /// </summary>
        public static string HostName
        {
            get
            {
                if (s_HostName != null) return s_HostName;
                s_HostName = GetHostName();
                return s_HostName;
            }                
        }
    }
}
