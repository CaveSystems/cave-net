using System;
using System.Runtime.InteropServices;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Provides the standard NTP timestamp format described in RFC-1305 and
    /// previous versions of that document.In conformance with standard
    /// Internet practice, NTP data are specified as integer or fixed-point
    /// quantities, with bits numbered in big - endian fashion from 0 starting
    /// at the left, or high - order, position.Unless specified otherwise, all
    /// quantities are unsigned and may occupy the full field width with an
    /// implied 0 preceding bit 0.
    /// <para>
    /// Since NTP timestamps are cherished data and, in fact, represent the
    /// main product of the protocol, a special timestamp format has been
    /// established.NTP timestamps are represented as a 64-bit unsigned
    /// fixed-point number, in seconds relative to 0h on 1 January 1900.The
    /// integer part is in the first 32 bits and the fraction part in the
    /// last 32 bits.In the fraction part, the non - significant low order can
    /// be set to 0.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <pre>
    ///                    1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                           Seconds                             |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                  Seconds Fraction(0-padded)                   |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+.
    /// </pre>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
    public struct NtpTimestamp : IEquatable<NtpTimestamp>
    {
        /// <summary>Returns the result of <paramref name="left"/>. <see cref="Equals(NtpTimestamp)"/>( <paramref name="right"/>).</summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>True if the values are equal, false otherwise</returns>
        public static bool operator ==(NtpTimestamp left, NtpTimestamp right) => left.Equals(right);

        /// <summary>Returns the result of ! <paramref name="left"/>. <see cref="Equals(NtpTimestamp)"/>( <paramref name="right"/>).</summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>False if the values are equal, true otherwise</returns>
        public static bool operator !=(NtpTimestamp left, NtpTimestamp right) => !(left == right);

        /// <summary>Provides the seconds per epoch.</summary>
        public const long FullEpoch = 1L << 32;

        /// <summary>Provides the seconds per quarter epoch.</summary>
        public const long QuarterEpoch = FullEpoch >> 2;

        /// <summary>Provides the start of the ntp epoch.</summary>
        public static readonly DateTime NtpEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static Func<DateTime> localReferenceTimeFunction = () => DateTime.UtcNow;

        /// <summary>Gets a timestamp with Seconds and Fraction set to 0.</summary>
        public static NtpTimestamp Zero => default;

        /// <summary>Gets or sets a function to retrieve the local reference time. This is needed after the original ntp timestamp overflows in 2036.</summary>
        public static Func<DateTime> LocalReferenceTimeFunction { get => localReferenceTimeFunction; set => localReferenceTimeFunction = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>Implicit conversion from <see cref="System.DateTime"/> to <see cref="NtpTimestamp"/>.</summary>
        /// <param name="dateTime">The dateTime value to convert.</param>
        public static implicit operator NtpTimestamp(DateTime dateTime) => new() { DateTime = dateTime };

        static void GetBaseEpoch(out DateTime baseEpoch, out long secondsOffset)
        {
            // split into quarters and move base fordward until we are sure we got the correct timeframe based on the current year. This works as long as we do
            // not exceed a difference of more than (1 << 30) seconds.
            baseEpoch = NtpEpoch;
            var currentSeconds = (LocalReferenceTimeFunction() - baseEpoch).Ticks / TimeSpan.TicksPerSecond;
            var i = 0;
            while (currentSeconds > QuarterEpoch)
            {
                currentSeconds -= QuarterEpoch;
                i++;
            }

            baseEpoch += new TimeSpan(QuarterEpoch * i * TimeSpan.TicksPerSecond);

            // calculate seconds offset for start at base epoch
            secondsOffset = -(QuarterEpoch * (uint)(i % 4));
        }

        /// <summary>Gets or sets the dateTime this instance represents.</summary>
        public DateTime DateTime
        {
            get
            {
                GetBaseEpoch(out var baseEpoch, out var secondsOffset);

                // check range
                var secondsSinceBaseEpoch = Seconds + secondsOffset;
                return secondsSinceBaseEpoch is < (-QuarterEpoch) or > QuarterEpoch
                    ? throw new Exception($"Local clock is more than {QuarterEpoch / 86400} days out of sync!")
                    : baseEpoch + new TimeSpan((TimeSpan.TicksPerSecond * secondsSinceBaseEpoch) + (TimeSpan.TicksPerSecond * Fraction / FullEpoch));
            }
            set
            {
                var baseEpoch = NtpEpoch;
                var secondsSinceEpoch = (value - baseEpoch).Ticks / TimeSpan.TicksPerSecond;
                Fraction = (uint)(value.Ticks % TimeSpan.TicksPerSecond * FullEpoch / TimeSpan.TicksPerSecond);
                while (secondsSinceEpoch >= FullEpoch)
                {
                    secondsSinceEpoch -= FullEpoch;
                }

                Seconds = (uint)secondsSinceEpoch;
            }
        }

        /// <summary>Gets or sets the seconds since ntp epoch.</summary>
        public NtpUInt32 Seconds;

        /// <summary>Gets or sets the fraction part of the timestamp.</summary>
        public NtpUInt32 Fraction;

        /// <summary>Gets the raw ntp timespan without epoch.</summary>
        public TimeSpan TimeSpan => new((TimeSpan.TicksPerSecond * Seconds) + (TimeSpan.TicksPerSecond * Fraction / 0x100000000L));

        /// <summary>Gets the <see cref="DateTime"/> as string.</summary>
        /// <returns>Returns a string that represents the current object.</returns>
        public override string ToString() => DateTime.ToString();

        /// <summary>Serves as the default hash function.</summary>
        /// <returns>Retruns a hash code for the current object.</returns>
        public override int GetHashCode() => DateTime.GetHashCode();

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is NtpTimestamp other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(NtpTimestamp other) => Seconds == other.Seconds && Fraction == other.Fraction;
    }
}
