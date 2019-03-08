using System.Net;

namespace Cave.Net
{
    /// <summary>
    /// Provides a udp packet implementation
    /// </summary>
    public class UdpPacket
    {
        /// <summary>
        /// The source this packet was received from (may be null)
        /// </summary>
        public IPEndPoint LocalEndPoint { get; internal protected set; }

        /// <summary>
        /// The source or destination of this packet
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; internal protected set; }

        /// <summary>
        /// Data of the packet (without udp/ip header)
        /// </summary>
        public byte[] Data { get; internal protected set; }

        /// <summary>
        /// Offset the data starts.
        /// </summary>
        public ushort Offset { get; internal protected set; }

        /// <summary>
        /// Size of the packet (without udp/ip header)
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
