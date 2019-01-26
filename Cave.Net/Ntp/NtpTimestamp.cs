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
    ///                      1                   2                   3
    ///  0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                           Seconds                             |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
    /// |                  Seconds Fraction(0-padded)                   |
    /// +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 8)]
    public struct NtpTimestamp
    {
        const long fullEpoch = 1L << 32;
        const long quarterEpoch = fullEpoch >> 2;
        static readonly DateTime ntpEpoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        static Func<DateTime> localReferenceTimeFunction = () => DateTime.UtcNow;

        /// <summary>
        /// Gets a timestamp with Seconds and Fraction set to 0.
        /// </summary>
        public static NtpTimestamp Zero => default(NtpTimestamp);

        /// <summary>
        /// Gets or sets a function to retrieve the local reference time. This is needed after the original ntp timestamp overflows in 2036.
        /// </summary>
        public static Func<DateTime> LocalReferenceTimeFunction { get => localReferenceTimeFunction; set => localReferenceTimeFunction = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Implicit conversion from <see cref="System.DateTime"/> to <see cref="NtpTimestamp"/>.
        /// </summary>
        /// <param name="dateTime">The dateTime value to convert.</param>
        public static implicit operator NtpTimestamp(DateTime dateTime) => new NtpTimestamp() { DateTime = dateTime };

        void GetBaseEpoch(out DateTime baseEpoch, out long secondsOffset)
        {
            // split into quarters and move base fordward until we are sure we got the correct timeframe based on the current year. This works as long as we do not exceed a difference of more than (1 << 30) seconds.
            baseEpoch = ntpEpoch;
            var currentSeconds = (LocalReferenceTimeFunction() - baseEpoch).Ticks / TimeSpan.TicksPerSecond;
            var i = 0;
            while (currentSeconds > quarterEpoch)
            {
                currentSeconds -= quarterEpoch;
                i++;
            }

            baseEpoch += new TimeSpan(quarterEpoch * i * TimeSpan.TicksPerSecond);

            // calculate seconds offset for start at base epoch
            secondsOffset = -(quarterEpoch * (uint)(i % 4));
        }

        /// <summary>
        /// Gets or sets the dateTime this instance represents.
        /// </summary>
        public DateTime DateTime
        {
            get
            {
                GetBaseEpoch(out DateTime baseEpoch, out var secondsOffset);

                // check range
                var secondsSinceBaseEpoch = Seconds + secondsOffset;
                if (secondsSinceBaseEpoch < -quarterEpoch || secondsSinceBaseEpoch > quarterEpoch)
                {
                    throw new Exception($"Local clock is more than {quarterEpoch / 86400} days out of sync!");
                }

                return baseEpoch + new TimeSpan((TimeSpan.TicksPerSecond * secondsSinceBaseEpoch) + (TimeSpan.TicksPerSecond * Fraction / fullEpoch));
            }
            set
            {
                DateTime baseEpoch = ntpEpoch;
                var secondsSinceEpoch = (value - baseEpoch).Ticks / TimeSpan.TicksPerSecond;
                Fraction = (uint)((value.Ticks % TimeSpan.TicksPerSecond) * fullEpoch / TimeSpan.TicksPerSecond);
                while (secondsSinceEpoch >= fullEpoch)
                {
                    secondsSinceEpoch -= fullEpoch;
                }

                Seconds = (uint)secondsSinceEpoch;
            }
        }

        /// <summary>
        /// Gets or sets the seconds since ntp epoch.
        /// </summary>
        public NtpUInt32 Seconds;

        /// <summary>
        /// Gets or sets the fraction part of the timestamp.
        /// </summary>
        public NtpUInt32 Fraction;

        /// <summary>
        /// Gets the raw ntp timespan without epoch.
        /// </summary>
        public TimeSpan TimeSpan => new TimeSpan((TimeSpan.TicksPerSecond * Seconds) + (TimeSpan.TicksPerSecond * Fraction / 0x100000000L));

        /// <summary>
        /// Gets the <see cref="DateTime"/> as string.
        /// </summary>
        /// <returns>Returns a string that represents the current object.</returns>
        public override string ToString() => DateTime.ToString();

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>Retruns a hash code for the current object.</returns>
        public override int GetHashCode() => DateTime.GetHashCode();

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>Returns true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Equals(ToString(), obj?.ToString());
    }
}
