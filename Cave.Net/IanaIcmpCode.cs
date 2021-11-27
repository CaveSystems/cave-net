namespace Cave.Net
{
    /// <summary>Assigned Internet Protocol Numbers - Last Updated 2015-01-06 http://www.iana.org/assignments/protocol-numbers/protocol-numbers.xhtml.</summary>
    public enum IanaIcmpDestinationUnreachableCode
    {
        /// <summary>Network Unreachable</summary>
        Network = 0,

        /// <summary>Host Unreachable</summary>
        Host,

        /// <summary>Protocol Unreachable</summary>
        Protocol,

        /// <summary>Port Unreachable</summary>
        Port,

        /// <summary>Fragmentation Needed and Don't Fragment was Set</summary>
        Fragmentation,

        /// <summary>Source Route Failed</summary>
        SourceRoute,

        /// <summary>Destination Network Unknown</summary>
        DestinationNetworkUnknown,

        /// <summary>Destination Host Unknown</summary>
        DestinationHostUnknown,

        /// <summary>Source Host Isolated</summary>
        SourceHostIsolated,

        /// <summary>Communication with Destination Network is Administratively Prohibited</summary>
        NetworkProhibited,

        /// <summary>Communication with Destination Host is Administratively Prohibited</summary>
        HostProhibited,

        /// <summary>Destination Network Unreachable for Type of Service</summary>
        ServiceNetworkUnreachable,

        /// <summary>Destination Host Unreachable for Type of Service</summary>
        ServiceHostUnreachable,

        /// <summary>Communication Administratively Prohibited</summary>
        CommunicationProhibited,

        /// <summary>Host Precedence Violation</summary>
        HostPrecedenceViolation,

        /// <summary>Precedence cutoff in effect</summary>
        PrecedenceCutoff,
    }
}
