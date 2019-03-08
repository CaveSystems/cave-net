using System;

namespace Cave.Net.Dns
{
    /// <summary>
    /// DNS Header Flags
    /// In DNS query header there is a flag field in the second 16 bit word in query from bit 5 through bit 11 ([RFC1035] section 4.1.1)
    /// </summary>
    [Flags]
    public enum DnsFlags
    {
        /// <summary>The mask for the response code (bit 0-3)</summary>
        MaskResponseCode = 0xf,

        // 3 bits zero (bit 4-6)

        /// <summary>The checking disabled flag (bit 5)</summary>
        CheckingDisabled = 1 << 4,

        /// <summary>The authentic data flag (bit 6)</summary>
        AuthenticData = 1 << 5,

        /// <summary>A reserved bit</summary>
        ReservedBit6 = 1 << 6,

        /// <summary>The recursion available flag (bit 8)</summary>
        RecursionAvailable = 1 << 7,

        /// <summary>The recursion desired flag (bit 9)</summary>
        RecursionDesired = 1 << 8,

        /// <summary>The truncated response flag (bit 10)</summary>
        TruncatedResponse = 1 << 9,

        /// <summary>The authoritive answer flag (bit 11)</summary>
        AuthoritiveAnswer = 1 << 10,

        /// <summary>The mask for opcodes</summary>
        MaskOpcode = 0xf << 11,

        /// <summary>The is response flag</summary>
        IsResponse = 1 << 15,

        /// <summary>The mask for all valid bits</summary>
        MaskFlags = 0xffff & ~(MaskOpcode | MaskResponseCode),
    }
}
