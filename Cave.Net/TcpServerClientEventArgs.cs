using System;

namespace Cave.Net
{
    /// <summary>
    /// Provides Event Arguments for the <see cref="TcpServer{TClient}"/> events.
    /// </summary>
    /// <typeparam name="TClient">The type of the client.</typeparam>
    /// <seealso cref="EventArgs" />
    public class TcpServerClientEventArgs<TClient> : EventArgs
    {
        /// <summary>Gets the client.</summary>
        /// <value>The client.</value>
        public TClient Client { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="TcpServerClientEventArgs{TClient}"/> class.</summary>
        /// <param name="client">The client.</param>
        public TcpServerClientEventArgs(TClient client) => Client = client;
    }
}
