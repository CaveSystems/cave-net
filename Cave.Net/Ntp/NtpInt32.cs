using System.Net;
using System.Runtime.InteropServices;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Provides an ntp int 32 value.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct NtpInt32
    {
        /// <summary>
        /// Implicit conversion from <see cref="NtpInt32"/> to int.
        /// </summary>
        /// <param name="val">Value to convert.</param>
        public static implicit operator int(NtpInt32 val) => val.Value;

        /// <summary>
        /// Implicit conversion from int to <see cref="NtpInt32"/>.
        /// </summary>
        /// <param name="val">Value to convert.</param>
        public static implicit operator NtpInt32(int val) => new() { Value = val };

        int value;

        /// <summary>
        /// Gets or sets the current value.
        /// </summary>
        public int Value { get => IPAddress.NetworkToHostOrder(value); set => this.value = IPAddress.HostToNetworkOrder(value); }

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
