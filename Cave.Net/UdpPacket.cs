using System;
using System.Net;

namespace Cave.Net
{
    /// <summary>
    /// Provides a udp packet implementation.
    /// </summary>
    public class UdpPacket
    {
        /// <summary>
        /// Gets or sets the source this packet was received from (may be null).
        /// </summary>
        public IPEndPoint LocalEndPoint { get; protected internal set; }

        /// <summary>
        /// Gets or sets the source or destination of this packet.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; protected internal set; }

        /// <summary>
        /// Gets or sets data of the packet (without udp/ip header).
        /// </summary>
        public byte[] Data { get; protected internal set; }

        /// <summary>
        /// Gets or sets offset the data starts.
        /// </summary>
        public ushort Offset { get; protected internal set; }

        /// <summary>
        /// Gets or sets the size of the packet (without udp/ip header).
        /// </summary>
        public ushort Size { get; protected internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacket"/> class.
        /// </summary>
        public UdpPacket()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacket"/> class.
        /// </summary>
        /// <param name="local">Local endpoint.</param>
        /// <param name="remote">Remote endpoint.</param>
        /// <param name="data">Data.</param>
        /// <param name="offset">Offset.</param>
        /// <param name="size">Size of data.</param>
        public UdpPacket(IPEndPoint local, IPEndPoint remote, byte[] data, ushort offset, ushort size)
        {
            LocalEndPoint = local ?? throw new ArgumentNullException(nameof(local));
            RemoteEndPoint = remote ?? throw new ArgumentNullException(nameof(remote));
            Data = data ?? throw new ArgumentNullException(nameof(data));
            Offset = offset;
            Size = size;
        }
    }
}
