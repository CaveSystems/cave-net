using System.Net;

namespace Cave.Net;

/// <summary>Provides event arguments with remote endpoint and buffer data.</summary>
public class RemoteEndPointBufferEventArgs : BufferEventArgs
{
    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="RemoteEndPointBufferEventArgs" /> class.</summary>
    /// <param name="remoteEndPoint">The remote endpoint.</param>
    /// <param name="buffer">The buffer containing the received data.</param>
    /// <param name="offset">Byte offset the received data starts.</param>
    /// <param name="length">Length in bytes the of the received data.</param>
    public RemoteEndPointBufferEventArgs(IPEndPoint remoteEndPoint, byte[] buffer, int offset, int length)
        : base(buffer, offset, length) => RemoteEndPoint = remoteEndPoint;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets or sets remote endpoint the buffer was received from.</summary>
    public IPEndPoint RemoteEndPoint { get; set; }

    #endregion Public Properties
}
