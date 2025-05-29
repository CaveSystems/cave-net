using System;
using System.Net;

namespace Cave.Net;

/// <summary>Provides <see cref="EventArgs"/> for remote endpoint.</summary>
public class IPEndPointEventArgs : EventArgs
{
    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="IPEndPointEventArgs"/> class.</summary>
    /// <param name="endPoint">IP endpoint.</param>
    public IPEndPointEventArgs(IPEndPoint endPoint) => EndPoint = endPoint;

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the endpoint.</summary>
    public IPEndPoint EndPoint { get; private set; }

    #endregion Public Properties
}
