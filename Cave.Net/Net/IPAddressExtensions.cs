using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cave.IO;

namespace Cave.Net;

/// <summary>Provides extensions to the <see cref="IPAddress"/> class.</summary>
public static class IPAddressExtensions
{
    #region Public Methods

    /// <summary>Except localhost ip addresses</summary>
    /// <param name="addresses">Addresses to filter</param>
    /// <returns>Returns the filtered enumeration</returns>
    public static IEnumerable<IPAddress> ExceptLocalhost(this IEnumerable<IPAddress> addresses) => addresses.Where(a => !a.IsLocalhost());

    /// <summary>Order by Loopback, Private, Other.</summary>
    /// <param name="addresses">Addresses to order</param>
    /// <returns>Returns the ordered enumeration</returns>
    public static IEnumerable<IPAddress> OrderPrivateFirst(this IEnumerable<IPAddress> addresses) => addresses.OrderBy(PrivateFirstOrder);

    /// <summary>Gets a order integer for the specified address.</summary>
    /// <param name="address"></param>
    /// <returns></returns>
    public static int PrivateFirstOrder(IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            if (bytes[0] == 172) return -3;
            if (bytes[0] == 10 || bytes[0] == 192) return -2;
            return -1;
        }
        var i = 0;
#if NET5_0_OR_GREATER
        if (address.IsIPv6UniqueLocal) i |= 1;
        i <<= 1;
#endif
        if (address.IsIPv6Multicast) i |= 1;
        i <<= 1;
        if (address.IsIPv6SiteLocal) i |= 1;
        i <<= 1;
        if (!address.IsIPv6LinkLocal) i |= 1;
        i <<= 1;
        return i;
    }

#endregion Public Methods
}
