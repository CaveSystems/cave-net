using System;

namespace Cave.Net.Dns;

/// <summary>Provides available dns servers</summary>
[Flags]
public enum DnsServerSelection : uint
{
    /// <summary>No settings</summary>
    None = 0,

    /// <summary>Use ipv4 addresses of nameservers</summary>
    V4 = 1 << 0,

    /// <summary>Use ipv6 addresses of nameservers</summary>
    V6 = 1 << 1,

    /// <summary>Quad9 nameserver</summary>
    Quad9 = 1 << 2,

    /// <summary>Cloudflare nameserver</summary>
    Cloudflare = 1 << 3,

    /// <summary>Google nameserver</summary>
    Google = 1 << 4,

    /// <summary>Cisco OpenDNS nameserver</summary>
    CiscoOpenDns = 1 << 5,

    /// <summary>DNS.Watch nameserver</summary>
    DnsWatch = 1 << 6,

    /// <summary>All known nameservers</summary>
    Everything = 0xFFFFFFFF
}
