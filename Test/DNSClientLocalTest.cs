using Cave;
using Cave.Net.Dns;
using NUnit.Framework;
using System.Net;

namespace Tests
{
    /// <summary>
    /// Test require a local dns server with the zone file provided in zone.txt
    /// </summary>
    class DNSClientLocalTest
    {
        IPAddress ServerAddress = new IPAddress(new byte[] { 127, 0, 0, 1 });

        [Test]
        public void TcpATest()
        {
            A_Test(new DnsClient() { UseTcp = true, UseUdp = false, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }

        [Test]
        public void TcpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = true, UseUdp = false, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }

        [Test]
        public void TcpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = true, UseUdp = false, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }

        [Test]
        public void TcpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = true, UseUdp = false, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }


        [Test]
        public void UdpATest()
        {
            A_Test(new DnsClient() { UseTcp = false, UseUdp = true, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }

        [Test]
        public void UdpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = false, UseUdp = true, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }

        [Test]
        public void UdpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = false, UseUdp = true, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }

        [Test]
        public void UdpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = false, UseUdp = true, Port = 8053, Servers = new IPAddress[] { ServerAddress } });
        }

        void CheckResponseIP(DnsRecord record,  string ipAddress)
        {
            IPAddress address;
            IPAddress.TryParse(ipAddress, out address);
            Assert.AreEqual(address, record.Value);
        }

        void A_Test(DnsClient testClient)
        {
            DnsResponse response = testClient.Resolve("example.com.", DnsRecordType.A);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.AreEqual(1, response.Answers.Count);
            CheckResponseIP(response.Answers[0], "192.0.2.1");
        }

        void AAAA_Test(DnsClient testClient)
        {
            DnsResponse response = testClient.Resolve("example.com.", DnsRecordType.AAAA);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.AreEqual(1, response.Answers.Count);
            CheckResponseIP(response.Answers[0], "2001:db8:10::1");
        }

        void MX_Test(DnsClient testClient)
        {
            DnsResponse response = testClient.Resolve("example.com.", DnsRecordType.MX);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            int counter = 0;
            foreach (var record in response.Answers)
            {
                Assert.AreEqual(DnsRecordType.MX, record.RecordType);
                Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                if (record.Value.ToString() == "10, mail.example.com") counter++;
                if (record.Value.ToString() == "20, mail2.example.com") counter++;
                if (record.Value.ToString() == "50, mail3.example.com") counter++;
            }
            Assert.AreEqual(3, counter);
        }

        void TXT_Test(DnsClient testClient)
        {

            DnsResponse response = testClient.Resolve("example.com.", DnsRecordType.TXT);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            foreach (var record in response.Answers)
            {
                Assert.AreEqual(DnsRecordType.TXT, record.RecordType);
                Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                if (record.Value.ToString() == "\"v=spf1 mx -all\"")
                {
                    return;
                }
            }
            Assert.Fail();
        }
    }
}