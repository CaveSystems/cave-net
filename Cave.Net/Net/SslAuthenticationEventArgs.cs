using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Cave.Net;

/// <summary>Provides <see cref="EventArgs" /> for <see cref="SslServer.Authenticate" /> events.</summary>
public class SslAuthenticationEventArgs : EventArgs
{
    #region Private Fields

    bool validated = true;

    #endregion Private Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="SslAuthenticationEventArgs" /> class.</summary>
    /// <param name="sslClient">The local <see cref="SslClient" /> receiving the event.</param>
    /// <param name="certificate">The remote certificate to be authorized.</param>
    /// <param name="chain">The remote certificate chain to be authorized.</param>
    /// <param name="sslPolicyErrors">The detected <see cref="SslPolicyErrors" />.</param>
    /// <param name="sslValidationErrors">The detected SSL validation errors.</param>
    public SslAuthenticationEventArgs(SslClient sslClient, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, SslValidationErrors sslValidationErrors)
    {
        SslClient = sslClient;
        Certificate = certificate;
        Chain = chain;
        SslPolicyErrors = sslPolicyErrors;
        SslValidationErrors = sslValidationErrors;
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the remote certificate to be authorized.</summary>
    public X509Certificate2 Certificate { get; private set; }

    /// <summary>Gets the remote certificate chain to be authorized.</summary>
    public X509Chain Chain { get; private set; }

    /// <summary>Gets the local <see cref="SslClient" /> receiving the event.</summary>
    public SslClient SslClient { get; private set; }

    /// <summary>Gets the detected <see cref="SslPolicyErrors" />.</summary>
    public SslPolicyErrors SslPolicyErrors { get; private set; }

    /// <summary>Gets the SSL validation errors.</summary>
    public SslValidationErrors SslValidationErrors { get; private set; }

    /// <summary>
    /// Gets or sets a value indicating whether authorization if successful or not. Setting this value to false does not allow to set it
    /// to true again.
    /// </summary>
    public bool Validated
    {
        get => validated;
        set => validated &= value;
    }

    #endregion Public Properties
}
