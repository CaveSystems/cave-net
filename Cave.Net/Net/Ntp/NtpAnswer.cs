using System;

namespace Cave.Net.Ntp;

/// <summary>Provides the result of a ntp query.</summary>
public sealed class NtpAnswer
{
    #region Internal Constructors

    internal NtpAnswer(NtpPacket answer)
    {
        Answer = answer;
        RoundTripDelay = DestinationTimestamp - answer.OriginateTimestamp.DateTime - (answer.ReceiveTimestamp.DateTime - answer.TransmitTimestamp.DateTime);
        LocalClockOffset = new(((answer.ReceiveTimestamp.DateTime - answer.OriginateTimestamp.DateTime) + (answer.TransmitTimestamp.DateTime - DestinationTimestamp)).Ticks / 2);
    }

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>Gets the received answer.</summary>
    public NtpPacket Answer { get; }

    /// <summary>Gets the dateTime the answer was received.</summary>
    public DateTime DestinationTimestamp { get; } = DateTime.UtcNow;

    /// <summary>Gets the local clock offset.</summary>
    public TimeSpan LocalClockOffset { get; }

    /// <summary>Gets the round trip delay.</summary>
    public TimeSpan RoundTripDelay { get; }

    #endregion Public Properties
}
