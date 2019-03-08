using System;

namespace Cave.Net
{
    /// <summary>
    /// Provides <see cref="EventArgs"/> for packet events.
    /// </summary>
    public class UdpPacketEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the packet.
        /// </summary>
        public UdpPacket Packet { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacketEventArgs"/> class.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public UdpPacketEventArgs(UdpPacket packet)
        {
            Packet = packet;
        }
    }
}
