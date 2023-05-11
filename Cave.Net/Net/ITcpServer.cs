using System;
using System.Net;
using System.Threading;

namespace Cave.Net;

/// <summary>Provides a TcpServer interface.</summary>
/// <seealso cref="IDisposable" />
public interface ITcpServer : IDisposable
{
    #region Public Properties

    /// <summary>Gets or sets the maximum length of the pending connections queue.</summary>
    /// <value>The maximum length of the pending connections queue.</value>
    /// <exception cref="InvalidOperationException">Socket is already listening.</exception>
    int AcceptBacklog { get; set; }

    /// <summary>Gets or sets the size of the buffer used when receiving data.</summary>
    /// <value>The size of the buffer.</value>
    /// <exception cref="InvalidOperationException">Socket is already listening.</exception>
    /// <exception cref="ArgumentOutOfRangeException">value.</exception>
    int BufferSize { get; set; }

    /// <summary>Gets a value indicating whether this instance is listening.</summary>
    /// <value><c>true</c> if this instance is listening; otherwise, <c>false</c>.</value>
    bool IsListening { get; }

    /// <summary>Gets or sets the amount of time, in milliseconds, thata read operation blocks waiting for data.</summary>
    /// <value>
    /// A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value,
    /// <see
    ///     cref="Timeout.Infinite" />
    /// , specifies that the read operation does not time out.
    /// </value>
    int ReceiveTimeout { get; set; }

    /// <summary>Gets or sets the amount of time, in milliseconds, thata write operation blocks waiting for data.</summary>
    /// <value>
    /// A Int32 that specifies the amount of time, in milliseconds, that will elapse before a write operation fails. The default value,
    /// <see
    ///     cref="Timeout.Infinite" />
    /// , specifies that the write operation does not time out.
    /// </value>
    int SendTimeout { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes the server and performs shutdown on all clients.</summary>
    void Close();

    /// <summary>Listens at the specified end point.</summary>
    /// <param name="endPoint">The end point.</param>
    /// <exception cref="ObjectDisposedException">TcpSocketServer.</exception>
    void Listen(IPEndPoint endPoint);

    /// <summary>Listens at the specified port.</summary>
    /// <param name="port">The port.</param>
    /// <exception cref="ObjectDisposedException">TcpSocketServer.</exception>
    void Listen(int port);

    #endregion Public Methods
}
