using System;

namespace Cave.Net.Ntp;

/// <summary>Provides an interface for ntp server properties.</summary>
public interface INtpServerProperties
{
    #region Public Properties

    /// <summary>Gets the current clock value.</summary>
    DateTime DateTime { get; }

    /// <summary>
    /// Gets an eight-bit signed integer indicating the maximum interval between successive messages, in seconds to the nearest power of
    /// two.The values that can appear in this field presently range from 4 (16 s) to 14 (16284 s); however, most applications use only the
    /// sub-range 6 (64 s) to 10 (1024 s).
    /// </summary>
    NtpPow2Int8 PollInterval { get; }

    /// <summary>
    /// Gets an eight-bit signed integer indicating the precision of the local clock, in seconds to the nearest power of two. The values
    /// that normally appear in this field range from -6 for mains-frequency clocks to -20 for microsecond clocks found in some workstations.
    /// </summary>
    NtpPow2Int8 Precision { get; }

    /// <summary>
    /// Gets a 32-bit bitstring identifying the particular reference source. In the case of NTP Version 3 or Version 4 stratum-0
    /// (unspecified) or stratum-1 (primary) servers, this is a four-character ASCII string, left justified and zero padded to 32 bits.In NTP
    /// Version 3 secondary servers, this is the 32-bit IPv4 address of the reference source.In NTP Version 4 secondary servers, this is the low
    /// order 32 bits of the latest transmit timestamp of the reference source.NTP primary (stratum 1) servers should set this field to a code
    /// identifying the external reference source according to the following list. If the external reference is one of those listed, the associated
    /// code should be used. Codes for sources not listed can be contrived as appropriate.
    /// </summary>
    NtpUInt32 Reference { get; }

    /// <summary>Gets the time at which the local clock was last set or corrected, in 64-bit timestamp format.</summary>
    NtpTimestamp ReferenceTimestamp { get; }

    /// <summary>
    /// Gets a 32-bit signed fixed-point number indicating the total roundtrip delay to the primary reference source, in seconds with
    /// fraction point between bits 15 and 16. Note that this variable can take on both positive and negative values, depending on the relative
    /// time and frequency offsets.The values that normally appear in this field range from negative values of a few milliseconds to positive
    /// values of several hundred milliseconds.
    /// </summary>
    NtpFixedPointInt32 RootDelay { get; }

    /// <summary>
    /// Gets a 32-bit unsigned fixed-point number indicating the nominal error relative to the primary reference source, in seconds with
    /// fraction point between bits 15 and 16. The values that normally appear in this field range from 0 to several hundred milliseconds.
    /// </summary>
    NtpFixedPointUInt32 RootDispersion { get; }

    /// <summary>Gets the Stratum of the clock.</summary>
    byte Stratum { get; }

    /// <summary>Gets a value indicating whether the clock is synchronized (valid, true) or not (invalid, false).</summary>
    bool Valid { get; }

    #endregion Public Properties
}
