using System;
using System.Net;

namespace Cave.Net;

/// <summary>Provides event arguments with remote endpoint.</summary>
public class RemoteEndPointEventArgs : EventArgs
{
    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="RemoteEndPointEventArgs"/> class.</summary>
    /// <param name="remoteEndPoint">The remote endpoint.</param>
    public RemoteEndPointEventArgs(IPEndPoint remoteEndPoint) => RemoteEndPoint = remoteEndPoint;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets or sets remote endpoint causing the event.</summary>
    public IPEndPoint RemoteEndPoint { get; set; }

    #endregion Public Properties
}
