using System;
using System.Runtime.InteropServices;

namespace Cave.Net
{
    /// <summary>
    /// Provides a fast TcpServer implementation using a user defined client class.
    /// </summary>
    /// <seealso cref="IDisposable" />
    /// <typeparam name="TClient">The TcpAsyncClient{this}implementation to be used for client instances.</typeparam>
    [ComVisible(false)]
    public class TypedTcpServer<TClient> : TcpServer<TClient>
        where TClient : TcpAsyncClient<TypedTcpServer<TClient>>, new()
    {
    }
}
