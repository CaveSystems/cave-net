using System;
using System.Net;
using System.Net.Sockets;

namespace Cave.Net
{
    /// <summary>
    /// Provides a udp packet sender
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

        readonly bool UsesServerSocket;
        Socket m_Socket;
        bool m_Closed;

        /// <summary>
        /// Obtains the remote endpoint this client is primarily "connected" to. Be aware that udp does not support
        /// connections the way tcp does.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Obtains the number of bytes a package may contain maximally until it may get fragmented
        /// </summary>
        public readonly int MaximumPayloadSize;

        /// <summary>
        /// Obtains the local IPEndPoint
        /// </summary>
        public IPEndPoint LocalEndPoint => (IPEndPoint)m_Socket.LocalEndPoint;

        /// <summary>
        /// Provides access to the last activity date time
        /// </summary>
        public DateTime LastActivity { get; set; }

        /// <summary>
        /// Sets the maximum payload size for the specified address family
        /// </summary>
        /// <param name="addressFamily"></param>
        UdpPacketClient(AddressFamily addressFamily)
        {
            LastActivity = DateTime.UtcNow;
            MaximumPayloadSize = 576; //see IETF RFC 1122
            //substract package header (ip header + udp header)
            switch (addressFamily)
            {
                case AddressFamily.InterNetwork: MaximumPayloadSize -= 20 + 8; break;
                case AddressFamily.InterNetworkV6: MaximumPayloadSize -= 40 + 8; break;
                default: throw new ArgumentException(string.Format("Unknown AddressFamily {0}", addressFamily), "addressFamily");
            }
        }

        /// <summary>
        /// Creates a new client for the specified server socket
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        /// <param name="serverSocket"></param>
        internal UdpPacketClient(IPEndPoint remoteEndPoint, Socket serverSocket)
            : this(GetAddressFamily(remoteEndPoint))
        {
            RemoteEndPoint = remoteEndPoint;
            m_Socket = serverSocket;
            UsesServerSocket = true;
            if (m_Socket.AddressFamily != remoteEndPoint.AddressFamily)
            {
                throw new ArgumentException("AddressFamily does not match!");
            }
        }

        /// <summary>
        /// Creates a new client
        /// </summary>
        /// <param name="remoteEndPoint"></param>
        public UdpPacketClient(IPEndPoint remoteEndPoint)
            : this(GetAddressFamily(remoteEndPoint))
        {
            m_Socket = new Socket(remoteEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                Blocking = true
            };
            RemoteEndPoint = remoteEndPoint;
        }

        /// <summary>
        /// Sends a packet to the default <see cref="RemoteEndPoint"/>
        /// </summary>
        /// <param name="data"></param>
        /// <param name="size"></param>
        public void Send(byte[] data, int size)
        {
            lock (m_Socket)
            {
                m_Socket.SendTo(data, size, SocketFlags.None, RemoteEndPoint);
            }
            LastActivity = DateTime.UtcNow;
        }

        /// <summary>
        /// Directly sends a packet
        /// </summary>
        /// <param name="packet"></param>
        public void Send(UdpPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            if (m_Closed)
            {
                throw new InvalidOperationException(string.Format("Client already closed!"));
            }

            if (packet.RemoteEndPoint != RemoteEndPoint)
            {
                throw new ArgumentException(string.Format("Invalid remote endpoint  specified !"));
            }

            lock (m_Socket)
            {
                m_Socket.SendTo(packet.Data, packet.Size, SocketFlags.None, packet.RemoteEndPoint);
            }
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
            if (m_Closed)
            {
                throw new InvalidOperationException(string.Format("Client already closed!"));
            }

            if (UsesServerSocket)
            {
                throw new InvalidOperationException(string.Format("This client is part of an UdpPacketServer."));
            }

            UdpPacket packet = new UdpPacket();
            EndPoint endPoint = m_Socket.LocalEndPoint;
            int bufferSize = m_Socket.Available > 0 ? m_Socket.Available : MaximumPayloadSize;
            packet.Data = new byte[bufferSize];
            packet.Size = (ushort)m_Socket.ReceiveFrom(packet.Data, ref endPoint);
            packet.ReceivedBy = this;
            packet.RemoteEndPoint = (IPEndPoint)endPoint;
            LastActivity = DateTime.UtcNow;
            return packet;
        }

        /// <summary>
        /// Obtains whether the client was closed or not
        /// </summary>
        public bool Closed => m_Closed;

        /// <summary>
        /// Checks for equality with another client
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            UdpPacketClient other = obj as UdpPacketClient;
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
            m_Closed = true;
            if (!UsesServerSocket)
            {
                m_Socket.Close();
            }
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public void Dispose()
        {
            if (m_Socket != null)
            {
                if (!UsesServerSocket)
                {
                    ((IDisposable)m_Socket).Dispose();
                }
                m_Socket = null;
            }
            GC.SuppressFinalize(this);
        }
    }
}
