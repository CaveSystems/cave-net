using System;

namespace Cave.Net
{
    /// <summary>
    /// Provides <see cref="EventArgs"/> for <see cref="SslClient"/> instances
    /// </summary>
    public class SslClientEventArgs : EventArgs
    {
        /// <summary>
        /// Provides access to the client
        /// </summary>
        public SslClient Client { get; private set; }

        /// <summary>
        /// Creates a new instance for the specified client
        /// </summary>
        /// <param name="client">The SslClient</param>
        public SslClientEventArgs(SslClient client)
        {
            Client = client;
        }
    }
}
