using Cave.Net;
using Cave.Net.Dns;
using NUnit.Framework;
using System.Linq;
using System.Net;

namespace Test.Dns
{
    [TestFixture]
    public class ResolveAllRemoteTest
    {

        [Test]
        public void TcpATest()
        {
            A_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = DnsClient.GetPulicDnsServers() });
        }

        [Test]
        public void TcpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = DnsClient.GetPulicDnsServers() });
        }

        [Test]
        public void TcpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = DnsClient.GetPulicDnsServers() });
        }

        [Test]
        public void TcpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = DnsClient.GetPulicDnsServers() }, false);
        }


        [Test]
        public void UdpATest()
        {
            A_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = DnsClient.GetPulicDnsServers() });
        }

        [Test]
        public void UdpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = DnsClient.GetPulicDnsServers() });
        }

        [Test]
        public void UdpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = DnsClient.GetPulicDnsServers() });
        }

        [Test]
        public void UdpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = DnsClient.GetPulicDnsServers() }, true);
        }
        [Test]
        public void TcpPTRTest()
        {
            PTR_Test(new DnsClient() { UseTcp = true, UseUdp = false, Servers = DnsClient.GetPulicDnsServers() });
        }

        [Test]
        public void UdpPTRTest()
        {
            PTR_Test(new DnsClient() { UseTcp = false, UseUdp = true, Servers = DnsClient.GetPulicDnsServers() });
        }

        static void PTR_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("1.1.1.1.in-addr.arpa", DnsRecordType.PTR);
            Assert.AreEqual(testClient.Servers.Length, responses.Count);
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count == 1));
            Assert.IsTrue(responses.Any());
            foreach (var response in responses)
            {
                foreach (var record in response.Answers)
                {
                    Assert.AreEqual((DomainName)"one.one.one.one", record.Value);
                }
            }
        }

        static void A_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("one.one.one.one", DnsRecordType.A);
            Assert.AreEqual(testClient.Servers.Length, responses.Count);
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count >= 1));
            Assert.IsTrue(responses.Any());
            var ipOne = new IPAddress(new byte[] { 1, 1, 1, 1 });
            foreach (var response in responses)
            {
                Assert.IsTrue(response.Answers.Any(a => Equals(a.Value, ipOne)));
            }
        }

        static void AAAA_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("one.one.one.one", DnsRecordType.AAAA);
            Assert.AreEqual(testClient.Servers.Length, responses.Count);
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count >= 1));
            Assert.IsTrue(responses.Any());
            Assert.IsTrue(IPAddress.TryParse("2606:4700:4700::1111", out var ipOne));
            foreach (var response in responses)
            {
                Assert.IsTrue(response.Answers.Any(a => Equals(a.Value, ipOne)));
            }
        }

        static void MX_Test(DnsClient testClient)
        {
            var responses = testClient.ResolveAll("google.com.", DnsRecordType.MX);
            Assert.AreEqual(testClient.Servers.Length, responses.Count);
            Assert.IsTrue(responses.All(r => r.ResponseCode == DnsResponseCode.NoError));
            Assert.IsTrue(responses.All(r => r.Answers.Count >= 1));
            foreach (var response in responses)
            {
                var flags = 0;
                foreach (var record in response.Answers)
                {
                    Assert.AreEqual(DnsRecordType.MX, record.RecordType);
                    Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                    if (record.Value.ToString() == "10, aspmx.l.google.com") flags ^= 1;
                    if (record.Value.ToString() == "20, alt1.aspmx.l.google.com") flags ^= 2;
                    if (record.Value.ToString() == "30, alt2.aspmx.l.google.com") flags ^= 4;
                    if (record.Value.ToString() == "40, alt3.aspmx.l.google.com") flags ^= 8;
                    if (record.Value.ToString() == "50, alt4.aspmx.l.google.com") flags ^= 16;
                }
                Assert.AreEqual(0x1f, flags);
            }
        }

        static void TXT_Test(DnsClient testClient, bool truncated)
        {
            var responses = testClient.ResolveAll("google.com.", DnsRecordType.TXT);
            Assert.AreEqual(testClient.Servers.Length, responses.Count);
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
    }
}
