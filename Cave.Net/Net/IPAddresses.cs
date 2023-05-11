using System.Net;

namespace Cave.Net;

/// <summary>Provides global ip addresses.</summary>
public static class IPAddresses
{
    #region Public Fields

    /// <summary>The ipv4 multicast address.</summary>
    public static readonly IPAddress IPv4MulticastAddress = IPAddress.Parse("224.0.0.0");

    /// <summary>The ipv6 multicast address.</summary>
    public static readonly IPAddress IPv6MulticastAddress = IPAddress.Parse("FF00::");

    #endregion Public Fields
}
