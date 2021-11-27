using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Cave.Net.Ntp
{
    /// <summary>Provides an ntp uint 32 value.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct NtpUInt32 : IEquatable<NtpUInt32>
    {
        /// <summary>Returns the result of <paramref name="left"/>. <see cref="Equals(NtpUInt32)"/>( <paramref name="right"/>).</summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>True if the values are equal, false otherwise</returns>
        public static bool operator ==(NtpUInt32 left, NtpUInt32 right) => left.Equals(right);

        /// <summary>Returns the result of ! <paramref name="left"/>. <see cref="Equals(NtpUInt32)"/>( <paramref name="right"/>).</summary>
        /// <param name="left">left operand</param>
        /// <param name="right">right operand</param>
        /// <returns>False if the values are equal, true otherwise</returns>
        public static bool operator !=(NtpUInt32 left, NtpUInt32 right) => !(left == right);

        /// <summary>Implicit conversion from <see cref="NtpInt32"/> to uint.</summary>
        /// <param name="val">Value to convert.</param>
        public static implicit operator uint(NtpUInt32 val) => val.Value;

        /// <summary>Implicit conversion from uint to <see cref="NtpInt32"/>.</summary>
        /// <param name="val">Value to convert.</param>
        public static implicit operator NtpUInt32(uint val) => new() { Value = val };

        uint value;

        /// <summary>Gets or sets the current value.</summary>
        public uint Value
        {
            get => (uint)IPAddress.NetworkToHostOrder((int)value);
            set => this.value = (uint)IPAddress.HostToNetworkOrder((int)value);
        }

        /// <summary>Gets the <see cref="Value"/> as string.</summary>
        /// <returns>Returns a string that represents the current object.</returns>
        public override string ToString() => Value.ToString();

        /// <inheritdoc/>
        public override int GetHashCode() => Value.GetHashCode();

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is NtpUInt32 other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(NtpUInt32 other) => other.Value == Value;
    }
}
