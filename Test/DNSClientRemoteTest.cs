using Cave;
using Cave.Net.Dns;
using NUnit.Framework;
using System.Net;

namespace Tests
{
    class DNSClientRemoteTest
    {

        [Test]
        public void TcpATest()
        {
            A_Test(new DnsClient() { UseTcp = true, UseUdp = false });
        }

        [Test]
        public void TcpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = true, UseUdp = false });
        }

        [Test]
        public void TcpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = true, UseUdp = false });
        }

        [Test]
        public void TcpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = true, UseUdp = false });
        }


        [Test]
        public void UdpATest()
        {
            A_Test(new DnsClient() { UseTcp = false, UseUdp = true });
        }

        [Test]
        public void UdpAAAATest()
        {
            AAAA_Test(new DnsClient() { UseTcp = false, UseUdp = true });
        }

        [Test]
        public void UdpMXTest()
        {
            MX_Test(new DnsClient() { UseTcp = false, UseUdp = true });
        }

        [Test]
        public void UdpTXTTest()
        {
            TXT_Test(new DnsClient() { UseTcp = false, UseUdp = true });
        }


        void A_Test(DnsClient testClient)
        {
            DnsResponse response = testClient.Resolve("one.one.one.one", DnsRecordType.A);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            IPAddress ipOne = new IPAddress(new byte[] { 1, 1, 1, 1 });
            foreach (var record in response.Answers)
            {
                if (record.Value.Equals(ipOne))
                {
                    return;
                }
            }
            Assert.Fail();
        }

        void AAAA_Test(DnsClient testClient)
        {
            DnsResponse response = testClient.Resolve("one.one.one.one", DnsRecordType.AAAA);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            IPAddress ipOne;
            IPAddress.TryParse("2606:4700:4700::1111", out ipOne);
            foreach (var record in response.Answers)
            {
                if (record.Value.Equals(ipOne))
                {
                    return;
                }
            }
            Assert.Fail();
        }

        void MX_Test(DnsClient testClient)
        {
            DnsResponse response = testClient.Resolve("google.com.", DnsRecordType.MX);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            int counter = 0;
            foreach (var record in response.Answers)
            {
                Assert.AreEqual(DnsRecordType.MX, record.RecordType);
                Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                if (record.Value.ToString() == "10, aspmx.l.google.com") counter++;
                if (record.Value.ToString() == "20, alt1.aspmx.l.google.com") counter++;
                if (record.Value.ToString() == "30, alt2.aspmx.l.google.com") counter++;
                if (record.Value.ToString() == "40, alt3.aspmx.l.google.com") counter++;
                if (record.Value.ToString() == "50, alt4.aspmx.l.google.com") counter++;
            }
            Assert.AreEqual(5, counter);
        }

        void TXT_Test(DnsClient testClient)
        {
            DnsResponse response = testClient.Resolve("google.com.", DnsRecordType.TXT);
            Assert.AreEqual(DnsResponseCode.NoError, response.ResponseCode);
            Assert.GreaterOrEqual(response.Answers.Count, 1);
            foreach (var record in response.Answers)
            {
                Assert.AreEqual(DnsRecordType.TXT, record.RecordType);
                Assert.AreEqual(DnsRecordClass.IN, record.RecordClass);
                if (record.Value.ToString() == "\"v=spf1 include:_spf.google.com ~all\"")
                {
                    return;
                }
            }
            Assert.Fail();
        }
    }
}