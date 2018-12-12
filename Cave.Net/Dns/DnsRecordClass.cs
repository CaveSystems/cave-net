namespace Cave.Net.DNS
{
    /// <summary>
    /// DNS record classes
    /// <see href="http://www.iana.org/assignments/dns-parameters/dns-parameters.xhtml" />
    /// </summary>
    public enum DnsRecordClass
    {
        /// <summary>Reserved</summary>
		Reserved = 0,

        /// <summary>Internet (IN)</summary>
        IN = 1,

        /// <summary>Unassigned</summary>
        Unassigned = 2,

        /// <summary>Chaos (CH)</summary>
        CH = 3,

        /// <summary>Hesiod (HS)</summary>
        HS = 4,

        //5-253 	0x0005-0x00FD 	Unassigned

        /// <summary>NONE</summary>
        None = 254,

        /// <summary>ANY</summary>
        Any = 255,

        //256-65279 	0x0100-0xFEFF 	Unassigned 	
        //65280-65534 	0xFF00-0xFFFE 	Reserved for Private Use
        //65535 	0xFFFF 	Reserved
    }
}
