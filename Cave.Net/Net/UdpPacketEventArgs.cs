using System;

namespace Cave.Net
{
    /// <summary>Provides <see cref="EventArgs"/> for packet events.</summary>
    public class UdpPacketEventArgs : EventArgs
    {
        #region Public Constructors

        /// <summary>Initializes a new instance of the <see cref="UdpPacketEventArgs"/> class.</summary>
        /// <param name="packet">The packet.</param>
        public UdpPacketEventArgs(UdpPacket packet) => Packet = packet;

        #endregion Public Constructors

        #region Public Properties

        /// <summary>Gets the packet.</summary>
        public UdpPacket Packet { get; private set; }

        #endregion Public Properties
    }
}
