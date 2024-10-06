using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Cave.Net.Dns;

namespace Cave.Net;

/// <summary>Network specific settings and addresses.</summary>
public static class NetTools
{
    #region Private Fields

    static string? myHostName;

    #endregion Private Fields

    #region Public Properties

    /// <summary>Gets the fqdn (hostname) caching the result.</summary>
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

    #endregion Public Properties

    #region Private Methods

    /// <summary>Obtains the fqdn (hostname) of the system.</summary>
    static string GetHostName()
    {
        string? hostName = null;
        {
            try
            {
                hostName = IPGlobalProperties.GetIPGlobalProperties().HostName;
            }
            catch { }
        }
        if (string.IsNullOrEmpty(hostName))
        {
            try
            {
                hostName = System.Net.Dns.GetHostName();
            }
            catch { }
        }
        if (string.IsNullOrEmpty(hostName))
        {
            try
            {
                hostName = Environment.MachineName;
            }
            catch { }
        }
        if (string.IsNullOrEmpty(hostName))
        {
            try
            {
                hostName = Environment.GetEnvironmentVariable("COMPUTERNAME");
            }
            catch { }
        }
        if (string.IsNullOrEmpty(hostName))
        {
            try
            {
                hostName = Environment.GetEnvironmentVariable("HOSTNAME");
            }
            catch { }
        }
        if (string.IsNullOrEmpty(hostName))
        {
            hostName = "localhost";
        }

        string? domainName = null;
        {
            try
            {
                domainName = IPGlobalProperties.GetIPGlobalProperties().DomainName;
            }
            catch { }
        }
        if (string.IsNullOrEmpty(domainName))
        {
            try
            {
                domainName = Environment.UserDomainName;
            }
            catch { }
        }
        if (string.IsNullOrEmpty(domainName))
        {
            domainName = "localdomain";
        }

        var names = new[]
        {
            hostName + "." + domainName,
            hostName
        };

        foreach (var str in names)
        {
            try
            {
                var entry = System.Net.Dns.GetHostEntry(str);
                myHostName = entry.HostName;
                return myHostName;
            }
            catch { }
        }

        // no network fallback
        return hostName + "." + domainName;
    }

    #endregion Private Methods

    #region Public Methods

    /// <summary>Obtains the first available <see cref="IPAddress"/> for the specified hostname with the specified <see cref="AddressFamily"/>.</summary>
    /// <param name="hostName">Hostname to get addresses for.</param>
    /// <param name="addressFamily">Address family to lookup.</param>
    /// <returns>Returns the first matching ip address.</returns>
    public static IPAddress GetAddress(string hostName, AddressFamily addressFamily)
    {
        foreach (var address in DnsClient.Default.GetHostAddresses(hostName))
        {
            if (address.AddressFamily == addressFamily)
            {
                return address;
            }
        }
        throw new ArgumentException(string.Format("Could not find an IPAddress for {0} with AddressFamily {1}!", hostName, addressFamily));
    }

    /// <summary>Obtains all available <see cref="IPAddress"/> for the specified hostname with the specified <see cref="AddressFamily"/>.</summary>
    /// <param name="hostName">Hostname to get addresses for.</param>
    /// <param name="addressFamily">Address family to lookup.</param>
    /// <returns>Returns a new array of matching ip addresses.</returns>
    public static IPAddress[] GetAddresses(string hostName, AddressFamily addressFamily)
    {
        var addresses = new List<IPAddress>();
        foreach (var address in DnsClient.Default.GetHostAddresses(hostName))
        {
            if (address.AddressFamily == addressFamily)
            {
                addresses.Add(address);
            }
        }
        return [.. addresses];
    }

    /// <summary>Tries to find a free tcp port.</summary>
    /// <param name="defaultPort">The default port.</param>
    /// <param name="maximumTries">The maximum tries.</param>
    /// <returns>Returns a free tcp port.</returns>
    public static ushort GetFreeTcpPort(int defaultPort, int maximumTries = 10)
    {
        var port = defaultPort;
        try
        {
#if NET45_OR_GREATER || NETSTANDARD2_0_OR_GREATER || NET5_0_OR_GREATER
            var listener = TcpListener.Create(port);
#else
            var listener = new TcpListener(IPAddress.Any, port);
#endif
            listener.Start();
            listener.Stop();
            return (ushort)port;
        }
        catch { }
        for (var i = 0; ; i++)
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
    /// Parses a string for a valid IPAddress[:Port] or DnsName[:Port] combination and retrieves all matching <see cref="IPEndPoint"/> s. If no port is
    /// specified DefaultPort will be returned.
    /// </summary>
    /// <param name="text">The string containing the ip endpoint (server[:port] or ipaddress[:port]).</param>
    /// <param name="defaultPort">The default port used if no port was given.</param>
    /// <returns>Returns an array of <see cref="IPEndPoint"/> s.</returns>
    public static IPEndPoint[] GetIPEndPoints(string text, int defaultPort)
    {
        if (string.IsNullOrEmpty(text)) return [];

        var port = defaultPort;
        var portIndex = text.LastIndexOf(':');
        if (portIndex > -1)
        {
            var portString = text[(portIndex + 1)..];
            if (int.TryParse(portString, out port))
            {
                text = text[..portIndex];
            }
        }

        if ((text == string.Empty) || (text == "any"))
        {
            return
            [
                new(IPAddress.Any, port),
                new(IPAddress.IPv6Any, port)
            ];
        }

        if (IPAddress.TryParse(text, out var a))
        {
            return [new(a, port)];
        }

        var result = new List<IPEndPoint>();
        foreach (var address in DnsClient.Default.GetHostAddresses(text))
        {
            result.Add(new(address, port));
        }
        return [.. result];
    }

    /// <summary>Retrieves all local addresses.</summary>
    /// <param name="status">Filter to apply.</param>
    /// <returns>Returns <see cref="UnicastIPAddressInformation"/> instances for all local network interfaces.</returns>
    public static UnicastIPAddressInformation[] GetLocalAddresses(OperationalStatus? status = null)
    {
        var result = new List<UnicastIPAddressInformation>();
        IEnumerable<NetworkInterface> interfaces = NetworkInterface.GetAllNetworkInterfaces();
        if (status != null)
        {
            interfaces = interfaces.Where(i => i.OperationalStatus == status.Value);
        }
        foreach (var ni in interfaces)
        {
            var p = ni.GetIPProperties();
            foreach (var ip in p.UnicastAddresses)
            {
                result.Add(ip);
            }
        }
        return [.. result];
    }

    /// <summary>Retrieves local addresses with the specified addresfamily.</summary>
    /// <param name="addressFamily">Address family used to lookup local addresses.</param>
    /// <returns>Returns a list of local ip addresses.</returns>
    public static IPAddress[] GetLocalAddresses(AddressFamily addressFamily)
    {
        var result = new List<IPAddress>();
        foreach (var addr in DnsClient.Default.GetHostAddresses(NetTools.HostName))
        {
            if (addr.AddressFamily == addressFamily)
            {
                result.Add(addr);
            }
        }
        return [.. result];
    }

    /// <summary>Determines whether the specified address is localhost.</summary>
    /// <param name="address">The address.</param>
    /// <returns><c>true</c> if the specified address is localhost; otherwise, <c>false</c>.</returns>
    public static bool IsLocalhost(this IPAddress address)
    {
        var stringAddress = address.ToString();
        return address.AddressFamily switch
        {
            AddressFamily.InterNetwork => stringAddress.StartsWith("127."),
            AddressFamily.InterNetworkV6 => stringAddress.StartsWith("::ffff:127.") || (stringAddress == "::1"),
            _ => false
        };
    }

    #endregion Public Methods
}
