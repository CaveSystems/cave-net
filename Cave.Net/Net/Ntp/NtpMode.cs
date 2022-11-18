using System;

namespace Cave.Net.Ntp
{
    /// <summary>This is a three-bit integer indicating the operation mode.</summary>
    public enum NtpMode
    {
        /// <summary>Invalid mode</summary>
        [Obsolete("Do not use this value")]
        Invalid = 0,

        /// <summary>Symmetric active</summary>
        SymmetricActive = 1,

        /// <summary>Symmetric pasive</summary>
        SymmetricPassive,

        /// <summary>Client</summary>
        Client,

        /// <summary>Server</summary>
        Server,

        /// <summary>Broadcast</summary>
        Broadcast,
    }
}
