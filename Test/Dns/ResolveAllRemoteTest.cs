using System.Linq;
using System.Net;
using Cave.Net;
using Cave.Net.Dns;
using NUnit.Framework;

namespace Test.Dns
{
    [TestFixture]
    public class ResolveAllRemoteTest
    {
        #region Private Methods

        static void A_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("one.one.one.one", DnsRecordType.A);
            Assert.IsTrue(responses.Any(), "No response!");
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count >= 1), "At least one response has no answer!");
            var ipOne = new IPAddress(new byte[] { 1, 1, 1, 1 });
            foreach (var response in responses)
            {
                Assert.IsTrue(response.Answers.Any(a => Equals(a.Value, ipOne)));
            }
        }

        static void AAAA_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("one.one.one.one", DnsRecordType.AAAA);
            Assert.IsTrue(responses.Any(), "No response!");
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count >= 1), "At least one response has no answer!");
            Assert.IsTrue(IPAddress.TryParse("2606:4700:4700::1111", out var ipOne));
            foreach (var response in responses)
            {
                Assert.IsTrue(response.Answers.Any(a => Equals(a.Value, ipOne)));
            }
        }

        static IPAddress[] GetDnsServers()
        {
            //azure build server does not support ipv6
            //azure build server refuses connection to some public dns, use specific ones
            return new IPAddress[]
            {
                IPAddress.Parse("1.1.1.1"),
                IPAddress.Parse("8.8.4.4"),
                IPAddress.Parse("8.8.8.8"),
            };
        }

        static void MX_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("google.com.", DnsRecordType.MX);
            Assert.IsTrue(responses.Any(), "No response!");
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count >= 1), "At least one response has no answer!");
            foreach (var response in responses)
            {
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
        }

        static void PTR_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("1.1.1.1.in-addr.arpa", DnsRecordType.PTR);
            Assert.IsTrue(responses.Any(), "No response!");
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count >= 1), "At least one response has no answer!");
            foreach (var response in responses)
            {
                foreach (var record in response.Answers)
                {
                    Assert.AreEqual((DomainName)"one.one.one.one", record.Value);
                }
            }
        }

        static void TXT_Test(DnsClient testClient, bool truncated)
        {
            var responses = testClient.ResolveAll("google.com.", DnsRecordType.TXT);
            Assert.IsTrue(responses.Any(), "No response!");
            Assert.IsTrue(responses.All(r => r.IsTruncatedResponse == truncated));
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            if (!truncated)
            {
                Assert.IsTrue(responses.All(r => r.Answers.Count >= 1));
            }
            foreach (var response in responses)
            {
                var present = false;
                foreach (var record in response.Answers)
                {
                    Assert.AreEqual(DnsRecordType.TXT, record.RecordType);
                    Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                    if (record.Value.ToString() == "\"v=spf1 include:_spf.google.com ~all\"")
                    {
                        present = true;
                    }
                }
                if (!truncated) Assert.IsTrue(present);
            }
        }

        #endregion Private Methods

        #region Public Methods

        [Test]
        public void TcpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = GetDnsServers() });
        }

        [Test]
        public void TcpATest()
        {
            A_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = GetDnsServers() });
        }

        [Test]
        public void TcpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = GetDnsServers() });
        }

        [Test]
        public void TcpPTRTest()
        {
            PTR_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = GetDnsServers() });
        }

        [Test]
        public void TcpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = GetDnsServers() }, false);
        }

        [Test]
        public void UdpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = GetDnsServers() });
        }

        [Test]
        public void UdpATest()
        {
            A_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = GetDnsServers() });
        }

        [Test]
        public void UdpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = GetDnsServers() });
        }

        [Test]
        public void UdpPTRTest()
        {
            PTR_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = GetDnsServers() });
        }

        [Test]
        public void UdpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = GetDnsServers() }, true);
        }

        #endregion Public Methods
    }
}
