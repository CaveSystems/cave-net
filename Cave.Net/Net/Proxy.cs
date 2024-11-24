using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace Cave.Net;

/// <summary>Provides proxy settings.</summary>
public record Proxy : IWebProxy
{
    public static implicit operator Proxy(ConnectionString connectionString)
    {
        var useHttps = connectionString.Protocol == "https";
        return new()
        {
            Credentials = (connectionString.Password != null) | (connectionString.UserName != null) ? connectionString.GetCredentials() : null,
            Host = connectionString.Server,
            UseHttps = useHttps,
            Port = useHttps ? 443 : 80
        };
    }

    public static implicit operator ConnectionString(Proxy proxy)
    {
        return new()
        {
            Protocol = proxy.UseHttps ? "https" : "http",
            Password = proxy.Credentials?.Password,
            UserName = proxy.Credentials?.UserName,
            Server = proxy.Host,
            Port = (proxy.UseHttps ? (ushort)443 : (ushort)80)
        };
    }

    /// <summary>Gets or sets the proxy credentials</summary>
    public NetworkCredential? Credentials { get; init; }

    /// <summary>Gets or sets the proxy host</summary>
    public string? Host { get; init; }

    /// <summary>Gets or sets the proxy port</summary>
    public int Port { get; init; }

    /// <summary>Gets or sets a value indicating whether the proxy uses https or http.</summary>
    public bool UseHttps { get; init; } = true;

    /// <summary>Gets the proxy uri.</summary>
    /// <returns>The Proxy uri.</returns>
    public Uri GetUri()
    {
        var builder = new UriBuilder
        {
            Scheme = UseHttps ? Uri.UriSchemeHttps : Uri.UriSchemeHttp,
            Host = Host,
            Port = Port,
            UserName = Credentials?.UserName,
            Password = Credentials?.Password,
        };
        return builder.Uri;
    }

    /// <inheritdoc/>
    public override string ToString() => GetUri().ToString();

    IWebProxy? cache;
    Uri? IWebProxy.GetProxy(Uri destination) => GetWebProxy().GetProxy(destination);
    ICredentials? IWebProxy.Credentials { get => GetWebProxy().Credentials; set => throw new NotSupportedException(); }
    IWebProxy GetWebProxy() => cache ??= new WebProxy(GetUri());
    bool IWebProxy.IsBypassed(Uri host) => GetWebProxy().IsBypassed(host);
}
