namespace Cave.Net
{
    /// <summary>Provides an async tcp client implementation for typed server instances.</summary>
    /// <typeparam name="TServer">The server intance type. This is used with <see cref="TypedTcpServer{TClient}"/>.</typeparam>
    public class TcpAsyncClient<TServer> : TcpAsyncClient
        where TServer : ITcpServer
    {
        #region Public Properties

        /// <summary>Gets the server instance this client belongs to. May be <c>null</c>.</summary>
        public new TServer Server => (TServer)base.Server;

        #endregion Public Properties
    }
}
