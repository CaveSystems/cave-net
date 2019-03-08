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
        public IPEndPoint LocalEndPoint { get; internal protected set; }

        /// <summary>
        /// Gets or sets the source or destination of this packet.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; internal protected set; }

        /// <summary>
        /// Gets or sets data of the packet (without udp/ip header).
        /// </summary>
        public byte[] Data { get; internal protected set; }

        /// <summary>
        /// Gets or sets offset the data starts.
        /// </summary>
        public ushort Offset { get; internal protected set; }

        /// <summary>
        /// Gets or sets the size of the packet (without udp/ip header).
        /// </summary>
        public ushort Size { get; internal protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacket"/> class.
        /// </summary>
        public UdpPacket()
        {
        }
    }
}
