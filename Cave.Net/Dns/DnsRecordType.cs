using System;

namespace Cave.Net.Dns
{
    /// <summary>
    /// DNS record types
    /// <see href="http://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml" />
    /// </summary>
    public enum DnsRecordType : ushort
    {
        /// <summary>Invalid record type</summary>
        Invalid = 0,

        /// <summary>Host address</summary>
        A = 1,

        /// <summary>Authoritatitve name server</summary>
        NS = 2,

        /// <summary>Mail destination server</summary>
        [Obsolete]
        MD = 3,

        /// <summary>Mail forwarder</summary>
        [Obsolete]
        MF = 4,

        /// <summary>Canonical name for an alias</summary>
        CNAME = 5,

        /// <summary>Start of zone of authority</summary>
        SOA = 6,

        /// <summary>Mailbox domain name</summary>
        MB = 7,

        /// <summary>Mail group member</summary>
        MG = 8,

        /// <summary>Mail rename domain name</summary>
        MR = 9,

        /// <summary>Null record</summary>
        NULL = 10,

        /// <summary>Well known services</summary>
        WKS = 11,

        /// <summary>Domain name pointer</summary>
        PTR = 12,

        /// <summary>Host information</summary>
        HINFO = 13,

        /// <summary>Mailbox or mail list information</summary>
        MINFO = 14, // not supported yet

        /// <summary>Mail exchange</summary>
        MX = 15,

        /// <summary>Text strings</summary>
        TXT = 16,

        /// <summary>Responsible person</summary>
        RP = 17,

        /// <summary>AFS data base location</summary>
        AFSDB = 18,

        /// <summary>X.25 PSDN address</summary>
        X25 = 19,

        /// <summary>ISDN address</summary>
        ISDN = 20,

        /// <summary>Route through</summary>
        RT = 21,

        /// <summary>NSAP address, NSAP style A record</summary>
        NSAP = 22,

        /// <summary>Domain name pointer, NSAP style</summary>
        NSAPPTR = 23,

        /// <summary>Security signature</summary>
        SIG = 24,

        /// <summary>Security Key</summary>
        KEY = 25,

        /// <summary>X.400 mail mapping information</summary>
        PX = 26,

        /// <summary>Geographical position</summary>
        GPOS = 27,

        /// <summary>IPv6 address</summary>
        AAAA = 28,

        /// <summary>Location information</summary>
        LOC = 29,

        /// <summary>Next domain</summary>
        [Obsolete]
        NXT = 30,

        /// <summary>Endpoint identifier</summary>
        EID = 31,

        /// <summary>Nimrod locator</summary>
        NIMLOC = 32,

        /// <summary>Server selector</summary>
        SRV = 33,

        /// <summary>ATM address</summary>
        ATMA = 34,

        /// <summary>Naming authority pointer</summary>
        NAPTR = 35,

        /// <summary>Key exchanger</summary>
        KX = 36,

        /// <summary>Certificate storage</summary>
        CERT = 37,

        /// <summary>The a6</summary>
        [Obsolete]
        A6 = 38,

        /// <summary>DNS Name Redirection</summary>
        DNAME = 39,

        /// <summary>Kitchen Sink Resource Record</summary>
        SINK = 40,

        /// <summary>Optional resource record</summary>
        OPT = 41,

        /// <summary>Address prefixes</summary>
        APL = 42,

        /// <summary>Delegation signer</summary>
        DS = 43,

        /// <summary>SSH key fingerprint</summary>
        SSHFF = 44,

        /// <summary>IPsec key storage</summary>
        IPSECKEY = 45,

        /// <summary>Record signature</summary>
        RRSIG = 46,

        /// <summary>Next owner</summary>
        NSEC = 47,

        /// <summary>DNS Key</summary>
        DNSKEY = 48,

        /// <summary>Dynamic Host Configuration Protocol (DHCP) Information</summary>
        DHCID = 49,

        /// <summary>Hashed next owner</summary>
        NSEC3 = 50,

        /// <summary>Hashed next owner parameter</summary>
        NSEC3PARAM = 51,

        /// <summary>Transport Layer Security (TLS) Protocol parameters</summary>
        TLSA = 52,

        /// <summary>Host identity protocol</summary>
        HIP = 55,

        /// <summary>descriptive information about the status of the zone</summary>
        NINFO = 56,

        /// <summary>application keys specifically for encryption of DNS resource records</summary>
        RKEY = 57,

        /// <summary>Trust anchor link</summary>
        TALink = 58,

        /// <summary>Child DS</summary>
        CDS = 59,

        /// <summary>Child DnsKey</summary>
        CDNSKEY = 60,

        /// <summary>OpenPGP Key</summary>
        OpenPGPKey = 61,

        /// <summary>Child-to-Parent Synchronization</summary>
        CSYNC = 62,

        /// <summary>Sender Policy Framework</summary>
        [Obsolete]
        SPF = 99,

        /// <summary>UINFO</summary>
        UInfo = 100,

        /// <summary>UID</summary>
        UID = 101,

        /// <summary>GID</summary>
        GID = 102,

        /// <summary>UNSPEC</summary>
        UNSPEC = 103,

        /// <summary>NID</summary>
        NID = 104,

        /// <summary>L32</summary>
        L32 = 105,

        /// <summary>L64</summary>
        L64 = 106,

        /// <summary>LP</summary>
        LP = 107,

        /// <summary>EUI48</summary>
        EUI48 = 108,

        /// <summary>EUI64</summary>
        EUI64 = 109,

        //Unassigned    110-248 	

        /// <summary>Transaction Key</summary>
        TKEY = 249,

        /// <summary>Transaction Signature</summary>
        TSIG = 250,

        /// <summary>incremental transfer</summary>
        IXFR = 251,

        /// <summary>transfer of an entire zone</summary>
        AXFR = 252,

        /// <summary>mailbox-related RRs (MB, MG or MR)</summary>
        MAILB = 253,

        /// <summary>mail agent RRs (OBSOLETE - see MX)</summary>
        [Obsolete]
        MAILA = 254,

        /// <summary>A request for all records the server/cache has available</summary>
        ANY = 255,

        /// <summary>Uniform Resource Identifier</summary>
        URI = 256,

        /// <summary>Certification Authority Restriction</summary>
        CAA = 257,

        /// <summary>Application Visibility and Control</summary>
        AVC = 258,

        //Unassigned 	259-32767

        /// <summary>DNSSEC Trust Authorities</summary>
        TA = 32768,

        /// <summary>DNSSEC Lookaside Validation</summary>
        DLV = 32769,

        //Unassigned 	32770-65279
        //Private use 	65280-65534
        //Reserved 	65535 	
    }
}