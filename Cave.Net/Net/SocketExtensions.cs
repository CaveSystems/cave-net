using System.Net.Sockets;

namespace Cave.Net;

/// <summary>Provides extensions to the socket class.</summary>
public static class SocketExtensions
{
    #region Public Methods

    /// <summary>Enables dual socket on ipv4 and ipv6.</summary>
    /// <param name="socket"></param>
    public static void EnableDualSocket(this Socket socket) => socket.SetSocketOption(Ipv6, Ipv6Only, false);

    #endregion Public Methods

    #region Public Fields

    /// <summary>SocketOptionLevel for IPv6 (not present in .Net 2.0)</summary>
    public const SocketOptionLevel Ipv6 = (SocketOptionLevel)41;

    /// <summary>SocketOptionName for IPv6 only setting (not present in .Net 2.0)</summary>
    public const SocketOptionName Ipv6Only = (SocketOptionName)27;

    #endregion Public Fields
}
