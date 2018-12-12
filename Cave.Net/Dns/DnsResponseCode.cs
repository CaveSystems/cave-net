namespace Cave.Net.Dns
{
    /// <summary>Dns return codes</summary>
    public enum DnsResponseCode : ushort
    {
        /// <summary>No error condition</summary>
        NoError = 0,

        /// <summary>The name server was unable to interpret the request due to a format error.</summary>
        FormatError = 1,

        /// <summary>
        /// The name server encountered an internal failure while processing this request, for example an operating system error or a forwarding timeout.
        /// </summary>
        ServerFailure = 2,

        /// <summary>Some name that ought to exist, does not exist.</summary>
        NameError = 3,

        /// <summary>The name server does not support the specified Opcode.</summary>
        NotImplemented = 4,

        /// <summary>The name server refuses to perform the specified operation for policy or security reasons.</summary>
        Refused = 5,

        /// <summary>YXDOMAIN: Some name that ought not to exist, does exist.</summary>
        DomainExistsError = 6,

        /// <summary>YXRRSET: Some RRset that ought not to exist, does exist.</summary>
        RecordExistsError = 7,

        /// <summary>NXRRSET: Some RRset that ought to exist, does not exist.</summary>
        RecordNotExistsError = 8,

        /// <summary>The server is not authoritative for the zone named in the Zone Section.</summary>
        NotAuthoritive = 9,

        /// <summary>The not zone</summary>
        NotZone = 10,

        /// <summary>TSIG signature failure</summary>
        BadSig = 16,

        /// <summary>Key not recognized</summary>
        BadKey = 17,

        /// <summary>Signature out of time window</summary>
        BadTime = 18,

        /// <summary>Bad TKEY mode</summary>
        BadMode = 19,

        /// <summary>Duplicate key name</summary>
        BadName = 20,

        /// <summary>Algorithm not supported</summary>
        BadAlg = 21,

        /// <summary>Bad truncation of TSIG record</summary>
        BadTrunc = 22,

        /// <summary>Bad/missing server cookie</summary>
        BadCookie = 23,
    }
}
