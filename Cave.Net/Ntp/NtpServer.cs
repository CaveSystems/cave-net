﻿using System;
using Cave.IO;
using Cave.Net;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Implements a rfc2030 ntp server.
    /// All incoming udp packets at port 123 are answered using values generated by the <see cref="PropertiesFunction"/>.
    /// </summary>
    public class NtpServer : UdpServer
    {
        class DefaultNtpProperties : INtpServerProperties
        {
            public bool Valid => true;

            public DateTime DateTime => DateTime.Now;

            public byte Stratum => 0x3e;

            public NtpPow2Int8 PollInterval => 6;

            public NtpPow2Int8 Precision => -1;

            public NtpFixedPointInt32 RootDelay => 1 << 16;

            public NtpFixedPointUInt32 RootDispersion => 1 << 16;

            public NtpUInt32 Reference => (uint)FourCC.FromString("LOCL");

            public NtpTimestamp ReferenceTimestamp => DateTime.Now;
        }

        Func<INtpServerProperties> propertiesFunction;

        bool Handle(UdpPacket packet)
        {
            var properties = propertiesFunction();
            var request = MarshalStruct.GetStruct<NtpPacket>(packet.Data);
            if (!OnRequest(ref request))
            {
                return false;
            }

            var answer = request;
            var multicast = false;
            answer.LeapIndicator = properties.Valid ? NtpLeapIndicator.NoWarning : NtpLeapIndicator.Alarm;
            switch (request.Mode)
            {
                case NtpMode.Client:
                {
                    answer.Mode = NtpMode.Server;
                    break;
                }

                default:
                {
                    multicast = packet.RemoteEndPoint.Address.IsMulticast();
                    answer.Mode = NtpMode.SymmetricPassive;
                    if (multicast)
                    {
                        answer.VersionNumber = 4;
                        answer.Mode = NtpMode.Broadcast;
                        answer.PollInterval = 6;
                    }

                    break;
                }
            }

            answer.Stratum = properties.Stratum;
            answer.PollInterval = properties.PollInterval;
            answer.Precision = properties.Precision;
            answer.RootDelay = properties.RootDelay;
            answer.RootDispersion = properties.RootDispersion;
            answer.Reference = properties.Reference;
            answer.ReferenceTimestamp = properties.ReferenceTimestamp;
            answer.TransmitTimestamp = properties.DateTime;
            if (multicast)
            {
                answer.OriginateTimestamp = NtpTimestamp.Zero;
                answer.ReceiveTimestamp = NtpTimestamp.Zero;
            }
            else
            {
                answer.OriginateTimestamp = request.TransmitTimestamp;
                answer.ReceiveTimestamp = properties.DateTime;
            }

            if (!OnAnswer(ref answer))
            {
                return false;
            }

            MarshalStruct.Write(answer, packet.Data, 0);
            return true;
        }

        /// <summary>
        /// Calls the base function, then handles the incoming packet.
        /// </summary>
        /// <param name="packet">The received udp packet.</param>
        protected override void OnReceived(UdpPacket packet)
        {
            base.OnReceived(packet);
            if (Handle(packet))
            {
                Send(packet);
            }
        }

        /// <summary>
        /// Calls the <see cref="Request"/> event and returns whether the request shall be handled (true) or not (false).
        /// </summary>
        /// <param name="request">Request packet.</param>
        /// <returns>Returns whether the request shall be handled (true) or not (false).</returns>
        protected virtual bool OnRequest(ref NtpPacket request)
        {
            if (Request != null)
            {
                var e = new NtpPacketEventArgs(request);
                Request?.Invoke(this, e);
                request = e.Packet;

                return !e.Discard;
            }

            return true;
        }

        /// <summary>
        /// Calls the <see cref="Answer"/> event and returns whether the answer shall be sent (true) or not (false).
        /// </summary>
        /// <param name="answer">Answer packet.</param>
        /// <returns>Returns whether the answer shall be sent (true) or not (false).</returns>
        protected virtual bool OnAnswer(ref NtpPacket answer)
        {
            if (Request != null)
            {
                var e = new NtpPacketEventArgs(answer);
                Answer?.Invoke(this, e);
                answer = e.Packet;

                return !e.Discard;
            }

            return true;
        }

        /// <summary>
        /// Gets or sets the <see cref="Func{INtpServerProperties}"/> used to retrieve the answer values.
        /// </summary>
        public Func<INtpServerProperties> PropertiesFunction { get => propertiesFunction; set => propertiesFunction = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Event to be called on each incoming request (before handling).
        /// </summary>
        public EventHandler<NtpPacketEventArgs> Request;

        /// <summary>
        /// Event to be called on each outgoing answer (before sending).
        /// </summary>
        public EventHandler<NtpPacketEventArgs> Answer;
    }
}
