using System;
using System.Runtime.InteropServices;

namespace Cave.Net.Ntp;

/// <summary>Provides a 32 bit ntp fixed point value with fraction point between bits 15 and 16.</summary>
[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
public struct NtpFixedPointUInt32
{
    /// <summary>Implicit conversion from uint to <see cref="NtpFixedPointUInt32" />.</summary>
    /// <param name="value">Value to convert.</param>
    public static implicit operator NtpFixedPointUInt32(uint value) => new() { Value = value };

    /// <summary>Gets or sets the current raw value.</summary>
    public NtpUInt32 Value;

    /// <summary>Gets the <see cref="Value" /> in seconds.</summary>
    public double Seconds => Value / (double)0x10000;

    /// <summary>Gets the timespan representing this instance.</summary>
    public TimeSpan Time => TimeSpan.FromSeconds(Seconds);

    /// <summary>Gets the <see cref="Value" /> as string.</summary>
    /// <returns>Returns a string that represents the current object.</returns>
    public override string ToString() => $"{Seconds}s";
}
