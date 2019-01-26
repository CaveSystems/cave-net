using System.Net;
using System.Runtime.InteropServices;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Provides an ntp uint 32 value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct NtpUInt32
    {
        /// <summary>
        /// Implicit conversion from <see cref="NtpInt32"/> to uint.
        /// </summary>
        /// <param name="val">Value to convert.</param>
        public static implicit operator uint(NtpUInt32 val) => val.Value;

        /// <summary>
        /// Implicit conversion from uint to <see cref="NtpInt32"/>.
        /// </summary>
        /// <param name="val">Value to convert.</param>
        public static implicit operator NtpUInt32(uint val) => new NtpUInt32() { Value = val };

        uint value;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public uint Value
        {
            get => (uint)IPAddress.NetworkToHostOrder((int)value);
            set => this.value = (uint)IPAddress.HostToNetworkOrder((int)value);
        }

        /// <summary>
        /// Gets the <see cref="Value"/> as string.
        /// </summary>
        /// <returns>Returns a string that represents the current object.</returns>
        public override string ToString() => Value.ToString();

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>Retruns a hash code for the current object.</returns>
        public override int GetHashCode() => Value.GetHashCode();

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns>Returns true if the specified object is equal to the current object; otherwise, false.</returns>
        public override bool Equals(object obj) => Equals(Value.ToString(), obj?.ToString());
    }
}
