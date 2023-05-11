using System;
using System.Net;
using System.Net.Sockets;
using Cave.IO;

namespace Cave.Net;

/// <summary>Provides event arguments containing the remote endpoint and exception.</summary>
public class RemoteEndPointExceptionEventArgs : ExceptionEventArgs
{
    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="RemoteEndPointExceptionEventArgs" /> class.</summary>
    /// <param name="remoteEndPoint">The remote endpoint causing the error. This may be null if the host encountered an error.</param>
    /// <param name="ex">The exception (most of the time this will be a <see cref="SocketException" />.</param>
    public RemoteEndPointExceptionEventArgs(IPEndPoint remoteEndPoint, Exception ex)
        : base(ex) => RemoteEndPoint = remoteEndPoint;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets or sets the remote endpoint causing the error. This may be null if the host encountered an error.</summary>
    public IPEndPoint RemoteEndPoint { get; set; }

    #endregion Public Properties
}
