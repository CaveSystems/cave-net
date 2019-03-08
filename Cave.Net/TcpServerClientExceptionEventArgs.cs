using System;

namespace Cave.Net
{
    /// <summary>
    /// Provides Event Arguments for the <see cref="TcpServer{TClient}.ClientException"/> event.
    /// </summary>
    /// <typeparam name="TClient">The type of the client.</typeparam>
    /// <seealso cref="EventArgs" />
    public class TcpServerClientExceptionEventArgs<TClient> : EventArgs
    {
        /// <summary>
        /// Gets the <see cref="Exception"/> that was encountered.
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>Gets the client.</summary>
        /// <value>The client.</value>
        public TClient Client { get; private set; }

        /// <summary>Initializes a new instance of the <see cref="TcpServerClientExceptionEventArgs{TClient}"/> class.</summary>
        /// <param name="client">The client.</param>
        /// <param name="ex">The <see cref="Exception"/> that was encountered.</param>
        public TcpServerClientExceptionEventArgs(TClient client, Exception ex)
        {
            Client = client;
            Exception = ex;
        }
    }
}
