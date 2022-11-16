using System.Linq;
using System.Net;
using Cave.Net;
using Cave.Net.Dns;
using NUnit.Framework;

namespace Test.Dns
{
    [TestFixture]
    public class DnsClientRemoteTest
    {
        #region Private Methods

        static void A_Test(DnsClient testClient)
        {
            var response = testClient.Resolve("one.one.one.one", DnsRecordType.A);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            var ipOne = new IPAddress(new byte[] { 1, 1, 1, 1 });
            foreach (var record in response.Answers)
            {
                if (record.Value.Equals(ipOne))
                {
                    return;
                }
            }
            Assert.Fail();
        }

        static void AAAA_Test(DnsClient testClient)
        {
            var response = testClient.Resolve("one.one.one.one", DnsRecordType.AAAA);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            Assert.IsTrue(IPAddress.TryParse("2606:4700:4700::1111", out var ipOne));
            Assert.IsTrue(response.Answers.Any(a => Equals(a.Value, ipOne)));
        }

        static void MX_Test(DnsClient testClient)
        {
            var response = testClient.Resolve("google.com.", DnsRecordType.MX);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            foreach (var record in response.Answers)
            {
                Assert.AreEqual(DnsRecordType.MX, record.RecordType);
                Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                if (record.Value is not MxRecord mx)
                {
                    Assert.Fail($"Record {record} is not an mx record!");
                }
                else
                {
                    Assert.AreEqual((DomainName)"smtp.google.com", mx.ExchangeDomainName);
                }
            }
        }

        static void PTR_Test(DnsClient testClient)
        {
            var response = testClient.Resolve("1.1.1.1.in-addr.arpa", DnsRecordType.PTR);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            foreach (var record in response.Answers)
            {
                if (record.Value.Equals((DomainName)"one.one.one.one"))
                {
                    return;
                }
            }
            Assert.Fail();
        }

        static void TXT_Test(DnsClient testClient, bool canBeTruncated)
        {
            var response = testClient.Resolve("google.com.", DnsRecordType.TXT);
            if (!canBeTruncated) Assert.IsFalse(response.IsTruncatedResponse);
            if (!response.IsTruncatedResponse) Assert.GreaterOrEqual(response.Answers.Count, 1);
            foreach (var record in response.Answers)
            {
                Assert.AreEqual(DnsRecordType.TXT, record.RecordType);
                Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                if (record.Value.ToString() == "\"v=spf1 include:_spf.google.com ~all\"")
                {
                    return;
                }
            }
            if (!canBeTruncated) Assert.Fail();
        }

        #endregion Private Methods

        #region Public Methods

        [Test]
        public void TcpAAAATest() => AAAA_Test(new DnsClient() { UseTcp = true, UseUdp = false });

        [Test]
        public void TcpATest() => A_Test(new DnsClient() { UseTcp = true, UseUdp = false });

        [Test]
        public void TcpMXTest() => MX_Test(new DnsClient() { UseTcp = true, UseUdp = false });

        [Test]
        public void TcpPTRTest() => PTR_Test(new DnsClient() { UseTcp = true, UseUdp = false });

        [Test]
        public void TcpTXTTest() => TXT_Test(new DnsClient() { UseTcp = true, UseUdp = false }, false);

        [Test]
        public void UdpAAAATest() => AAAA_Test(new DnsClient() { UseTcp = false, UseUdp = true });

        [Test]
        public void UdpATest() => A_Test(new DnsClient() { UseTcp = false, UseUdp = true });

        [Test]
        public void UdpMXTest() => MX_Test(new DnsClient() { UseTcp = false, UseUdp = true });

        [Test]
        public void UdpPTRTest() => PTR_Test(new DnsClient() { UseTcp = false, UseUdp = true });

        [Test]
        public void UdpTXTTest() => TXT_Test(new DnsClient() { UseTcp = false, UseUdp = true }, true);

        #endregion Public Methods
    }
}
