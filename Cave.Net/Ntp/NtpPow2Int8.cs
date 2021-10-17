using System;
using System.Runtime.InteropServices;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Provides a ntp power of two signed integer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 1)]
    public struct NtpPow2Int8
    {
        /// <summary>
        /// Implicit conversion from sbyte to <see cref="NtpPow2Int8"/>.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        public static implicit operator NtpPow2Int8(sbyte value) => new() { Value = value };

        /// <summary>
        /// Implicit conversion from <see cref="NtpPow2Int8"/> to TimeSpan.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        public static implicit operator TimeSpan(NtpPow2Int8 value) => value.Time;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public sbyte Value;

        /// <summary>
        /// Gets the time in seconds.
        /// </summary>
        public double Seconds => Math.Pow(2, Value);

        /// <summary>
        /// Gets the timespan.
        /// </summary>
        public TimeSpan Time => TimeSpan.FromSeconds(Seconds);

        /// <summary>
        /// Gets the time in seconds.
        /// </summary>
        /// <returns>Returns a string that represents the current object.</returns>
        public override string ToString() => $"{Seconds}s";
    }
}
