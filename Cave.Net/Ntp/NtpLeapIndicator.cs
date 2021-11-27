﻿namespace Cave.Net.Ntp
{
    /// <summary>This is a two-bit code state and warning of an impending leap second to be inserted/deleted in the last minute of the current day.</summary>
    public enum NtpLeapIndicator : byte
    {
        /// <summary>No warning</summary>
        NoWarning = 0,

        /// <summary>Last minute has 61 seconds</summary>
        LastMinute61,

        /// <summary>Last minute has 59 seconds</summary>
        LastMinute59,

        /// <summary>Alarm condition (clock not synchronized)</summary>
        Alarm,
    }
}
