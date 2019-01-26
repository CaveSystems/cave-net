using System;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Provides event arguments for handling ntp packets.
    /// </summary>
    public class NtpPacketEventArgs : EventArgs
    {
        bool discard;

        /// <summary>
        /// Gets or sets the ntp packet. Changes to this instance will be transfered to further processing.
        /// </summary>
        public NtpPacket Packet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether processing of the packet should stop and the package should be discarded.
        /// </summary>
        public bool Discard { get => discard; set => discard |= value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtpPacketEventArgs"/> class.
        /// </summary>
        /// <param name="packet">The packet.</param>
        public NtpPacketEventArgs(NtpPacket packet) => Packet = packet;
    }
}
