using System.Linq;
using System.Net;
using Cave.Net.Dns;

namespace SubnetScan
{
    internal class Info
    {
        public IPAddress Address { get; }
        public string Hostname { get; private set; }

        public Info(IPAddress address)
        {
            Address = address;
            try
            {
                var response = DnsClient.Default.ResolveSequential(Address);
                Hostname = response.Answers.First().Value.ToString();
            }
            catch { }
        }

        public override string ToString()
        {
            return $"{Address} {Hostname}";
        }
    }
}
