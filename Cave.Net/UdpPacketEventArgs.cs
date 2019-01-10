using System;

namespace Cave.Net
{
    /// <summary>
    /// Provides <see cref="EventArgs"/> for packet events
    /// </summary>
    public class UdpPacketEventArgs : EventArgs
    {
        /// <summary>
        /// Provides the packet
        /// </summary>
        public UdpPacket Packet { get; private set; }

        /// <summary>
        /// creates a new instance
        /// </summary>
        /// <param name="packet"></param>
        public UdpPacketEventArgs(UdpPacket packet)
        {
            Packet = packet;
        }
    }
}
