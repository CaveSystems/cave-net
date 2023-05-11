using System;
using System.Net;
using System.Runtime.InteropServices;

namespace Cave.Net.Ntp;

/// <summary>Provides an ntp int 32 value.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
public struct NtpInt32 : IEquatable<NtpInt32>
{
    /// <summary>Returns the result of <paramref name="left" />. <see cref="Equals(NtpInt32)" />( <paramref name="right" />).</summary>
    /// <param name="left">left operand</param>
    /// <param name="right">right operand</param>
    /// <returns>True if the values are equal, false otherwise</returns>
    public static bool operator ==(NtpInt32 left, NtpInt32 right) => left.Equals(right);

    /// <summary>Returns the result of ! <paramref name="left" />. <see cref="Equals(NtpInt32)" />( <paramref name="right" />).</summary>
    /// <param name="left">left operand</param>
    /// <param name="right">right operand</param>
    /// <returns>False if the values are equal, true otherwise</returns>
    public static bool operator !=(NtpInt32 left, NtpInt32 right) => !(left == right);

    /// <summary>Implicit conversion from <see cref="NtpInt32" /> to int.</summary>
    /// <param name="val">Value to convert.</param>
    public static implicit operator int(NtpInt32 val) => val.Value;

    /// <summary>Implicit conversion from int to <see cref="NtpInt32" />.</summary>
    /// <param name="val">Value to convert.</param>
    public static implicit operator NtpInt32(int val) => new() { Value = val };

    int value;

    /// <summary>Gets or sets the current value.</summary>
    public int Value { get => IPAddress.NetworkToHostOrder(value); set => this.value = IPAddress.HostToNetworkOrder(value); }

    /// <summary>Gets the <see cref="Value" /> as string.</summary>
    /// <returns>Returns a string that represents the current object.</returns>
    public override string ToString() => Value.ToString();

    /// <inheritdoc />
    public override int GetHashCode() => Value.GetHashCode();

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is NtpInt32 other && Equals(other);

    /// <inheritdoc />
    public bool Equals(NtpInt32 other) => Value == other.Value;
}
