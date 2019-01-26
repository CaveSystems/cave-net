using System;
using Cave.IO;
using Cave.Net.Ntp;
using NUnit.Framework;

namespace Tests
{
    public class NtpClientTest
    {
        [Test]
        public void TestMethod1()
        {
            for (uint u = 0; u < int.MaxValue; u = (u * 3) + 1)
            {
                int i = (int)u;
                Assert.AreEqual(u, (uint)(NtpUInt32)u);
                Assert.AreEqual(i, (int)(NtpInt32)i);
            }

            for (DateTime testDate = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc); testDate.Year < 5000; testDate += TimeSpan.FromHours(100.0 / 3.0))
            {
                NtpTimestamp.LocalReferenceTimeFunction = () => testDate;
                NtpTimestamp ntpTimestamp = testDate;
                Assert.IsTrue(Math.Abs(testDate.Ticks - ntpTimestamp.DateTime.Ticks) < 10);
            }

            NtpTimestamp.LocalReferenceTimeFunction = () => DateTime.UtcNow;

            DateTime testDateTime = DateTime.UtcNow;

            NtpPacket pack = new NtpPacket()
            {
                Settings = 0x1B,
                OriginateTimestamp = testDateTime,
                ReceiveTimestamp = testDateTime,
                ReferenceTimestamp = testDateTime,
                TransmitTimestamp = testDateTime,
                PollInterval = 4,
                Precision = 4,
                RootDelay = 5,
                RootDispersion = 6,
                Stratum = 16,
                Reference = (uint)FourCC.Create("LOCL"),
            };

            Assert.AreEqual(NtpLeapIndicator.NoWarning, pack.LeapIndicator);
            Assert.AreEqual(NtpMode.Client, pack.Mode);
            Assert.AreEqual(3, pack.VersionNumber);

            Assert.IsTrue(Math.Abs(testDateTime.Ticks - pack.OriginateTimestamp.DateTime.Ticks) < 10);
            Assert.IsTrue(Math.Abs(testDateTime.Ticks - pack.ReceiveTimestamp.DateTime.Ticks) < 10);
            Assert.IsTrue(Math.Abs(testDateTime.Ticks - pack.ReferenceTimestamp.DateTime.Ticks) < 10);
            Assert.IsTrue(Math.Abs(testDateTime.Ticks - pack.TransmitTimestamp.DateTime.Ticks) < 10);

            Assert.AreEqual(TimeSpan.FromSeconds(16), (TimeSpan)pack.PollInterval);
            Assert.AreEqual(16, pack.PollInterval.Seconds);

            pack.PollInterval = 6;
            Assert.AreEqual(64, pack.PollInterval.Seconds);
            Assert.AreEqual(TimeSpan.FromSeconds(64), (TimeSpan)pack.PollInterval);

            pack.PollInterval = 14;
            Assert.AreEqual(16384, pack.PollInterval.Seconds);
            Assert.AreEqual(TimeSpan.FromSeconds(16384), (TimeSpan)pack.PollInterval);

            NtpAnswer answer = NtpClient.Query("pool.ntp.org");
        }
    }
}
