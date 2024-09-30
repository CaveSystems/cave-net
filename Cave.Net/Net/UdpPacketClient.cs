using System;
using System.Net;
using System.Net.Sockets;

namespace Cave.Net;

/// <summary>Provides a udp packet client for synchronous packet sending.</summary>
public sealed class UdpPacketClient : IDisposable
{
    #region Private Fields

    readonly bool usesServerSocket;
    Socket? socket;

    #endregion Private Fields

    #region Private Constructors

    /// <summary>Initializes a new instance of the <see cref="UdpPacketClient"/> class.</summary>
    /// <param name="addressFamily">The address familiy.</param>
    UdpPacketClient(AddressFamily addressFamily)
    {
        LastActivity = DateTime.UtcNow;
        MaximumPayloadSize = 576; // see IETF RFC 1122

        // substract package header (ip header + udp header)
        MaximumPayloadSize -= addressFamily switch
        {
            AddressFamily.InterNetwork => 20 + 8,
            AddressFamily.InterNetworkV6 => 40 + 8,
            _ => throw new ArgumentException(string.Format("Unknown AddressFamily {0}", addressFamily), nameof(addressFamily))
        };
        RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    }

    #endregion Private Constructors

    #region Private Methods

    static AddressFamily GetAddressFamily(IPEndPoint endPoint) => endPoint == null ? throw new ArgumentNullException(nameof(endPoint)) : endPoint.AddressFamily;

    #endregion Private Methods

    #region Internal Constructors

    /// <summary>Initializes a new instance of the <see cref="UdpPacketClient"/> class.</summary>
    /// <param name="remoteEndPoint">The remote endpoint.</param>
    /// <param name="serverSocket">The server socket instance.</param>
    internal UdpPacketClient(IPEndPoint remoteEndPoint, Socket serverSocket)
        : this(GetAddressFamily(remoteEndPoint))
    {
        RemoteEndPoint = remoteEndPoint;
        socket = serverSocket;
        usesServerSocket = true;
        if (socket.AddressFamily != remoteEndPoint.AddressFamily)
        {
            throw new ArgumentException("AddressFamily does not match!");
        }
    }

    #endregion Internal Constructors

    #region Public Fields

    /// <summary>Gets the number of bytes a package may contain maximally until it may get fragmented.</summary>
    public readonly int MaximumPayloadSize;

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="UdpPacketClient"/> class.</summary>
    /// <param name="remoteEndPoint">Remote endpoint.</param>
    public UdpPacketClient(IPEndPoint remoteEndPoint)
        : this(GetAddressFamily(remoteEndPoint))
    {
        socket = new(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            Blocking = true
        };
        RemoteEndPoint = remoteEndPoint;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets a value indicating whether the client was closed or not.</summary>
    public bool Closed { get; private set; }

    /// <summary>Gets or sets access to the last activity date time.</summary>
    public DateTime LastActivity { get; set; }

    /// <summary>Gets the local IPEndPoint.</summary>
    public IPEndPoint LocalEndPoint => (socket?.LocalEndPoint as IPEndPoint) ?? throw new InvalidCastException("Could not cast LocalEndPoint!");

    /// <summary>Gets the remote endpoint this client is primarily "connected" to. Be aware that udp does not support connections the way tcp does.</summary>
    public IPEndPoint RemoteEndPoint { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes the <see cref="UdpPacketClient"/>.</summary>
    public void Close()
    {
        Closed = true;
        if (!usesServerSocket)
        {
            socket?.Close();
        }
    }

    /// <summary>Disposes this instance.</summary>
    public void Dispose()
    {
        if (socket is IDisposable disposable)
        {
            if (!usesServerSocket)
            {
                disposable.Dispose();
            }
            socket = null;
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>Determines whether two object instances are equal.</summary>
    /// <param name="obj">The object to compare with the current object.</param>
    /// <returns>true if the specified object is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object? obj) => obj is UdpPacketClient other && RemoteEndPoint.Equals(other.RemoteEndPoint);

    /// <summary>Returns a hash value for a System.Net.IPEndPoint instance.</summary>
    /// <returns>An integer hash value.</returns>
    public override int GetHashCode() => RemoteEndPoint.GetHashCode();

    /// <summary>
    /// Reads a packet from the client. It is not possible to use polling and events to receive packets. Use the PacketIncomingEvent or polling with Read()!
    /// Attention: UdpPacketClient may receive packets from any source no only the initial remote endpoint.
    /// </summary>
    /// <returns>An <see cref="UdpPacket"/> or null.</returns>
    public UdpPacket Read()
    {
        if (socket is null) throw new ObjectDisposedException(nameof(UdpPacketClient));
        if (Closed) throw new InvalidOperationException("Client already closed!");
        if (usesServerSocket) throw new InvalidOperationException("This client is part of an UdpPacketServer.");

        var packet = new UdpPacket
        {
            LocalEndPoint = (socket.LocalEndPoint as IPEndPoint) ?? throw new InvalidCastException("Could not cast LocalEndPoint")
        };
        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
        var bufferSize = socket.Available > 0 ? socket.Available : MaximumPayloadSize;
        packet.Data = new byte[bufferSize];
        packet.Size = (ushort)socket.ReceiveFrom(packet.Data, ref endPoint);
        packet.RemoteEndPoint = (IPEndPoint)endPoint;
        LastActivity = DateTime.UtcNow;
        return packet;
    }

    /// <summary>Sends a packet to the specified <paramref name="remote"/>.</summary>
    /// <param name="remote">Remote endpoint to send packet to.</param>
    /// <param name="data">Byte array to send.</param>
    /// <param name="offset">Offset at buffer to start sending at.</param>
    /// <param name="size">Number of bytes to send.</param>
    public void Send(IPEndPoint remote, byte[] data, int offset, int size)
    {
        if (socket is null) throw new ObjectDisposedException(nameof(UdpPacketClient));
        if (Closed) throw new InvalidOperationException("Client already closed!");
        lock (socket)
        {
            socket.SendTo(data, offset, size, SocketFlags.None, remote);
        }
        LastActivity = DateTime.UtcNow;
    }

    /// <summary>Sends a packet to the specified <paramref name="remote"/>.</summary>
    /// <param name="remote">Remote endpoint to send packet to.</param>
    /// <param name="data">Byte array to send.</param>
    /// <param name="size">Number of bytes to send.</param>
    public void Send(IPEndPoint remote, byte[] data, int size) => Send(remote, data, 0, size);

    /// <summary>Sends a packet to the specified <paramref name="remote"/>.</summary>
    /// <param name="remote">Remote endpoint to send packet to.</param>
    /// <param name="data">Byte array to send.</param>
    public void Send(IPEndPoint remote, byte[] data) => Send(remote, data, 0, data.Length);

    /// <summary>Sends a packet to the default <see cref="RemoteEndPoint"/>.</summary>
    /// <param name="data">Byte array to send.</param>
    /// <param name="offset">Offset at buffer to start sending at.</param>
    /// <param name="size">Number of bytes to send.</param>
    public void Send(byte[] data, int offset, int size) => Send(RemoteEndPoint, data, offset, size);

    /// <summary>Sends a packet to the default <see cref="RemoteEndPoint"/>.</summary>
    /// <param name="data">Byte array to send.</param>
    /// <param name="size">Number of bytes to send.</param>
    public void Send(byte[] data, int size) => Send(RemoteEndPoint, data, 0, size);

    /// <summary>Sends a packet to the default <see cref="RemoteEndPoint"/>.</summary>
    /// <param name="data">Byte array to send.</param>
    public void Send(byte[] data) => Send(RemoteEndPoint, data, 0, data.Length);

    /// <summary>Directly sends a packet.</summary>
    /// <param name="packet">Packet to send.</param>
    public void Send(UdpPacket packet)
    {
        if (packet == null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        Send(packet.Data, 0, packet.Size);
        LastActivity = DateTime.UtcNow;
    }

    #endregion Public Methods
}
