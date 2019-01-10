using System;
using System.Net;

namespace Cave.Net
{
    /// <summary>
    /// Provides <see cref="EventArgs"/> for remote endpoint
    /// </summary>
    public class IPEndPointEventArgs : EventArgs
    {
        /// <summary>
        /// provides the packet incoming
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        /// <summary>
        /// creates a new csPacketIncomingEventArgs object
        /// </summary>
        /// <param name="endPoint"></param>
        public IPEndPointEventArgs(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
    }
}
