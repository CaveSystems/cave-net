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
        public UdpPacketClient ReceivedBy;

        /// <summary>
        /// The source or destination of this packet
        /// </summary>
        public IPEndPoint RemoteEndPoint;

        /// <summary>
        /// Data of the packet (without udp/ip header)
        /// </summary>
        public byte[] Data;

        /// <summary>
        /// Size of the packet (without udp/ip header)
        /// </summary>
        public ushort Size;

        /// <summary>
        /// Creates an empty udp packet 
        /// </summary>
        public UdpPacket() { }
    }
}
