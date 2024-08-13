using System;
using System.Net;

namespace Cave.Net;

/// <summary>Provides proxy settings.</summary>
public class Proxy
{
    #region Public Properties

    /// <summary>Gets or sets the proxy credentials</summary>
    public NetworkCredential Credentials { get; set; }

    /// <summary>Gets or sets the proxy host</summary>
    public string Host { get; set; }

    /// <summary>Gets or sets the proxy port</summary>
    public int Port { get; set; }

    /// <summary>Gets or sets a value indicating whether the proxy uses https or http.</summary>
    public bool UseHttps { get; set; } = true;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Gets the proxy uri.</summary>
    /// <returns>The Proxy uri.</returns>
    public Uri GetUri()
    {
        var builder = new UriBuilder
        {
            Scheme = UseHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            Host = Host,
            Port = Port
        };
        return builder.Uri;
    }

    #endregion Public Methods
}
