using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;

namespace Cave.Net;

/// <summary>Provides proxy settings.</summary>
public record Proxy : BaseRecord, IWebProxy
{
    /// <summary>Converts a connection string structure to a proxy record instance</summary>
    /// <param name="connectionString"></param>
    public static implicit operator Proxy(ConnectionString connectionString)
    {
        var useHttps = connectionString.Protocol == "https";
        return new()
        {
            Credentials = (connectionString.Password != null) | (connectionString.UserName != null) ? connectionString.GetCredentials() : null,
            Host = connectionString.Server,
            UseHttps = useHttps,
            Port = connectionString.GetPort(useHttps ? 443 : 80)
        };
    }

    /// <summary>Converts a proxy record instance to a connection string structure</summary>
    /// <param name="proxy"></param>
    public static implicit operator ConnectionString(Proxy proxy)
    {
        return new()
        {
            Protocol = proxy.UseHttps ? "https" : "http",
            Password = proxy.Credentials?.Password,
            UserName = proxy.Credentials?.UserName,
            Server = proxy.Host,
            Port = (ushort)proxy.Port,
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

    /// <summary>List of addresses to ignore when using the proxy</summary>
    public ReadOnlyCollection<string> BypassList { get; init; } = new ReadOnlyCollection<string>([]);

    /// <summary>Protocol used</summary>
    public string Protocol => (UseHttps ? "https" : "http");

    IWebProxy? cache;
    Uri? IWebProxy.GetProxy(Uri destination) => GetWebProxy().GetProxy(destination);
    ICredentials? IWebProxy.Credentials { get => Credentials; set => throw new NotSupportedException(); }
    IWebProxy GetWebProxy() => cache ??= new WebProxy()
    {
        UseDefaultCredentials = false,
        Credentials = Credentials,
        Address = new Uri($"{Protocol}://{Host}:{Port}/"),
        BypassList = BypassList.ToArray(),
        BypassProxyOnLocal = false,
    };

    bool IWebProxy.IsBypassed(Uri host) => GetWebProxy().IsBypassed(host);
}
