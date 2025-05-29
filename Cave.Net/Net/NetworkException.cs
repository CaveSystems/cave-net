using System;

namespace Cave.Net;

/// <summary>Provides network exceptions.</summary>
/// <seealso cref="Exception"/>
public class NetworkException : Exception
{
    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="NetworkException"/> class. Message: Network problem.</summary>
    public NetworkException()
        : base("Network problem!") { }

    /// <summary>Initializes a new instance of the <see cref="NetworkException"/> class.</summary>
    /// <param name="msg">The message.</param>
    public NetworkException(string msg)
        : base(msg) { }

    /// <summary>Initializes a new instance of the <see cref="NetworkException"/> class.</summary>
    /// <param name="msg">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public NetworkException(string msg, Exception innerException)
        : base(msg, innerException) { }

    #endregion Public Constructors
}
