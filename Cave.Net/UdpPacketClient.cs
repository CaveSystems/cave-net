using System;
using System.Net;
using System.Net.Sockets;

namespace Cave.Net
{
    /// <summary>
    /// Provides a udp packet client for synchronous packet sending.
    /// </summary>
    public sealed class UdpPacketClient : IDisposable
    {
        static AddressFamily GetAddressFamily(IPEndPoint endPoint)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException("endPoint");
            }

            return endPoint.AddressFamily;
        }

        readonly bool usesServerSocket;
        Socket socket;
        bool closed;

        /// <summary>
        /// Gets the remote endpoint this client is primarily "connected" to. Be aware that udp does not support
        /// connections the way tcp does.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Gets the number of bytes a package may contain maximally until it may get fragmented
        /// </summary>
        public readonly int MaximumPayloadSize;

        /// <summary>
        /// Gets the local IPEndPoint
        /// </summary>
        public IPEndPoint LocalEndPoint => (IPEndPoint)socket.LocalEndPoint;

        /// <summary>
        /// Gets or sets access to the last activity date time
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacketClient"/> class.
        /// </summary>
        /// <param name="addressFamily">The address familiy</param>
        UdpPacketClient(AddressFamily addressFamily)
        {
            LastActivity = DateTime.UtcNow;
            MaximumPayloadSize = 576; // see IETF RFC 1122
            // substract package header (ip header + udp header)
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork: MaximumPayloadSize -= 20 + 8; break;
                case AddressFamily.InterNetworkV6: MaximumPayloadSize -= 40 + 8; break;
                default: throw new ArgumentException(string.Format("Unknown AddressFamily {0}", addressFamily), "addressFamily");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacketClient"/> class.
        /// </summary>
        /// <param name="remoteEndPoint">The remote endpoint</param>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacketClient"/> class.
        /// </summary>
        /// <param name="remoteEndPoint">Remote endpoint.</param>
        public UdpPacketClient(IPEndPoint remoteEndPoint)
            : this(GetAddressFamily(remoteEndPoint))
        {
            socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = true
            };
            RemoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// Sends a packet to the specified <paramref name="remote"/>
        /// </summary>
        /// <param name="remote">Remote endpoint to send packet to.</param>
        /// <param name="data">Byte array to send</param>
        /// <param name="offset">Offset at buffer to start sending at.</param>
        /// <param name="size">Number of bytes to send.</param>
        public void Send(IPEndPoint remote, byte[] data, int offset, int size)
        {
            if (closed)
            {
                throw new InvalidOperationException(string.Format("Client already closed!"));
            }

            lock (socket)
            {
                socket.SendTo(data, offset, size, SocketFlags.None, remote);
            }
            LastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Sends a packet to the specified <paramref name="remote"/>
        /// </summary>
        /// <param name="remote">Remote endpoint to send packet to.</param>
        /// <param name="data">Byte array to send</param>
        /// <param name="size">Number of bytes to send.</param>
        public void Send(IPEndPoint remote, byte[] data, int size) => Send(data, 0, size);

        /// <summary>
        /// Sends a packet to the specified <paramref name="remote"/>
        /// </summary>
        /// <param name="remote">Remote endpoint to send packet to.</param>
        /// <param name="data">Byte array to send</param>
        public void Send(IPEndPoint remote, byte[] data) => Send(data, 0, data.Length);

        /// <summary>
        /// Sends a packet to the default <see cref="RemoteEndPoint"/>
        /// </summary>
        /// <param name="data">Byte array to send</param>
        /// <param name="offset">Offset at buffer to start sending at.</param>
        /// <param name="size">Number of bytes to send.</param>
        public void Send(byte[] data, int offset, int size) => Send(RemoteEndPoint, data, offset, size);

        /// <summary>
        /// Sends a packet to the default <see cref="RemoteEndPoint"/>
        /// </summary>
        /// <param name="data">Byte array to send</param>
        /// <param name="size">Number of bytes to send.</param>
        public void Send(byte[] data, int size) => Send(RemoteEndPoint, data, 0, size);

        /// <summary>
        /// Sends a packet to the default <see cref="RemoteEndPoint"/>
        /// </summary>
        /// <param name="data">Byte array to send</param>
        public void Send(byte[] data) => Send(RemoteEndPoint, data, 0, data.Length);

        /// <summary>
        /// Directly sends a packet
        /// </summary>
        /// <param name="packet">Packet to send.</param>
        public void Send(UdpPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            Send(packet.Data, 0, packet.Size);
            LastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Reads a packet from the client. It is not possible to use polling and events to receive packets.
        /// Use the PacketIncomingEvent or polling with Read()!
        /// Attention: UdpPacketClient may receive packets from any source no only the initial remote endpoint.
        /// </summary>
        /// <returns>An <see cref="UdpPacket"/> or null</returns>
        public UdpPacket Read()
        {
            if (closed)
            {
                throw new InvalidOperationException(string.Format("Client already closed!"));
            }

            if (usesServerSocket)
            {
                throw new InvalidOperationException(string.Format("This client is part of an UdpPacketServer."));
            }

            var packet = new UdpPacket();
            packet.LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
            EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
            int bufferSize = socket.Available > 0 ? socket.Available : MaximumPayloadSize;
            packet.Data = new byte[bufferSize];
            packet.Size = (ushort)socket.ReceiveFrom(packet.Data, ref endPoint);
            packet.RemoteEndPoint = (IPEndPoint)endPoint;
            LastActivity = DateTime.UtcNow;
            return packet;
        }

        /// <summary>
        /// Obtains whether the client was closed or not
        /// </summary>
        public bool Closed => closed;

        /// <summary>
        /// Checks for equality with another client
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as UdpPacketClient;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return RemoteEndPoint.Equals(other.RemoteEndPoint);
        }

        /// <summary>
        /// Returns a hash value for a System.Net.IPEndPoint instance.
        /// </summary>
        /// <returns>An integer hash value.</returns>
        public override int GetHashCode() => RemoteEndPoint.GetHashCode();

        /// <summary>
        /// Closes the <see cref="UdpPacketClient"/>
        /// </summary>
        public void Close()
        {
            closed = true;
            if (!usesServerSocket)
            {
                socket.Close();
            }
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (socket != null)
            {
                if (!usesServerSocket)
                {
                    ((IDisposable)socket).Dispose();
                }
                socket = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
