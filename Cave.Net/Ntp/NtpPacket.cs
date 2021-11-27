using System.Net;
using System.Runtime.InteropServices;
using Cave.IO;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Structure of the standard NTP header (as described in RFC 2030).
    /// </summary>
    /// <remarks>
    /// <pre>
    ///                     1                   2                   3
    /// 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                          Root Delay                           |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                       Root Dispersion                         |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                     Reference Identifier                      |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Reference Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                   Originate Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Receive Timestamp (64)                     |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                    Transmit Timestamp (64)                    |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                 Key Identifier (optional) (32)                |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    ///  |                                                               |
    ///  |                                                               |
    ///  |                 Message Digest (optional) (128)               |
    ///  |                                                               |
    ///  |                                                               |
    ///  +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+.
    /// </pre>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 12 * 4)]
    public struct NtpPacket
    {
        /// <summary><see cref="LeapIndicator"/>, <see cref="VersionNumber"/>, <see cref="Mode"/>.</summary>
        public byte Settings;

        /// <summary>Gets or sets a two-bit code warning of an impending leap second to be inserted/deleted in the last minute of the current day.</summary>
        public NtpLeapIndicator LeapIndicator { get => (NtpLeapIndicator)(Settings >> 6); set => Settings = (byte)((Settings & ~(0x3 << 6)) | (((int)value & 0x3) << 6)); }

        /// <summary>
        /// Gets or sets a three-bit integer indicating the NTP/SNTP version number.The version number is 3 for Version 3 (IPv4
        /// only) and 4 for Version 4 (IPv4, IPv6 and OSI). If necessary to distinguish between IPv4, IPv6 and OSI, the encapsulating context must be inspected.
        /// </summary>
        public int VersionNumber { get => (Settings >> 3) & 0x7; set => Settings = (byte)((Settings & ~(0x7 << 3)) | ((value & 0x7) << 3)); }

        /// <summary>Gets or sets a three-bit integer indicating the mode.</summary>
        public NtpMode Mode { get => (NtpMode)(Settings & 0x7); set => Settings = (byte)((Settings & ~0x7) | ((int)value & 0x7)); }

        /// <summary>Gets or sets the Stratum of the clock.</summary>
        public byte Stratum;

        /// <summary>
        /// Gets or sets an eight-bit signed integer indicating the maximum interval between successive messages, in seconds to the nearest power of two.The
        /// values that can appear in this field presently range from 4 (16 s) to 14 (16284 s); however, most applications use only the sub-range 6 (64 s) to 10
        /// (1024 s).
        /// </summary>
        public NtpPow2Int8 PollInterval;

        /// <summary>
        /// Gets or sets an eight-bit signed integer indicating the precision of the local clock, in seconds to the nearest power of two. The values that
        /// normally appear in this field range from -6 for mains-frequency clocks to -20 for microsecond clocks found in some workstations.
        /// </summary>
        public NtpPow2Int8 Precision;

        /// <summary>
        /// Gets or sets a 32-bit signed fixed-point number indicating the total roundtrip delay to the primary reference source, in seconds with fraction point
        /// between bits 15 and 16. Note that this variable can take on both positive and negative values, depending on the relative time and frequency
        /// offsets.The values that normally appear in this field range from negative values of a few milliseconds to positive values of several hundred milliseconds.
        /// </summary>
        public NtpFixedPointInt32 RootDelay;

        /// <summary>
        /// Gets or sets a 32-bit unsigned fixed-point number indicating the nominal error relative to the primary reference source, in seconds with fraction
        /// point between bits 15 and 16. The values that normally appear in this field range from 0 to several hundred milliseconds.
        /// </summary>
        public NtpFixedPointUInt32 RootDispersion;

        /// <summary>
        /// Gets or sets a 32-bit bitstring identifying the particular reference source. In the case of NTP Version 3 or Version 4 stratum-0 (unspecified) or
        /// stratum-1 (primary) servers, this is a four-character ASCII string, left justified and zero padded to 32 bits.In NTP Version 3 secondary servers,
        /// this is the 32-bit IPv4 address of the reference source.In NTP Version 4 secondary servers, this is the low order 32 bits of the latest transmit
        /// timestamp of the reference source.NTP primary (stratum 1) servers should set this field to a code identifying the external reference source
        /// according to the following list. If the external reference is one of those listed, the associated code should be used. Codes for sources not listed
        /// can be contrived as appropriate.
        /// </summary>
        public NtpUInt32 Reference;

        /// <summary>
        /// Gets the reference <see cref="FourCC"/> or <see cref="IPAddress"/>.
        /// </summary>
        /// <remarks>
        /// <pre>
        /// Code     External Reference Source
        /// ----------------------------------------------------------------
        /// LOCL     uncalibrated local clock used as a primary reference for a subnet without external means of synchronization
        /// PPS      atomic clock or other pulse-per-second source individually calibrated to national standards
        /// ACTS     NIST dialup modem service
        /// USNO     USNO modem service
        /// PTB      PTB(Germany) modem service
        /// TDF      Allouis(France) Radio 164 kHz
        /// DCF      Mainflingen(Germany) Radio 77.5 kHz
        /// MSF      Rugby(UK) Radio 60 kHz
        /// WWV      Ft.Collins(US) Radio 2.5, 5, 10, 15, 20 MHz
        /// WWVB     Boulder(US) Radio 60 kHz
        /// WWVH     Kaui Hawaii(US) Radio 2.5, 5, 10, 15 MHz
        /// CHU      Ottawa(Canada) Radio 3330, 7335, 14670 kHz
        /// LORC     LORAN-C radionavigation system
        /// OMEG     OMEGA radionavigation system
        /// GPS      Global Positioning Service
        /// GOES     Geostationary Orbit Environment Satellite.
        /// </pre>
        /// </remarks>
        public string ReferenceString
        {
            get
            {
                if (Stratum < 2)
                {
                    if (VersionNumber is 3 or 4)
                    {
                        return ((FourCC)Reference.Value).ToString();
                    }
                }
                else
                {
                    if (VersionNumber == 3)
                    {
                        return new IPAddress(Reference.Value).ToString();
                    }
                }

                return null;
            }
        }

        /// <summary>Gets or sets the time at which the local clock was last set or corrected, in 64-bit timestamp format.</summary>
        public NtpTimestamp ReferenceTimestamp;

        /// <summary>Gets or sets the time at which the request departed the client for the server, in 64-bit timestamp format.</summary>
        public NtpTimestamp OriginateTimestamp;

        /// <summary>Gets or sets the time at which the request arrived at the server, in 64-bit timestamp format.</summary>
        public NtpTimestamp ReceiveTimestamp;

        /// <summary>Gets or sets the time at which the reply departed the server for the client, in 64-bit timestamp format.</summary>
        public NtpTimestamp TransmitTimestamp;
    }
}
