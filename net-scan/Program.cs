using Cave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace SubnetScan
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ThreadPool.SetMinThreads(1024, 1024);
                ThreadPool.SetMaxThreads(64000, 64000);
                new Program().Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void Run(string[] args)
        {
            if (args.Contains("/?") || args.Contains("--help"))
            {
                Help();
                return;
            }

            var dns = args.Contains("--dns");
            foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (i.OperationalStatus != OperationalStatus.Up) continue;
                if (i.NetworkInterfaceType == NetworkInterfaceType.Loopback) continue;
                var properties = i.GetIPProperties();
                if (dns && !(properties.IsDnsEnabled || properties.IsDynamicDnsEnabled)) continue;

                Console.WriteLine($"Adapter {i.Name} {i.NetworkInterfaceType} {i.Speed.FormatSize()}it {i.Id} {i.GetPhysicalAddress()} {i.Description}");
                Console.WriteLine($"DNS: {properties.DnsSuffix} {properties.DnsAddresses?.ToList().Join(' ')}");
                Console.WriteLine($"Gateway: {properties.GatewayAddresses?.ToList().Join(' ')}");
                foreach (var uni in properties.UnicastAddresses)
                {
#if NET45_OR_GREATER
                    Console.WriteLine($"Network {uni.Address.AddressFamily} {uni.Address}/{uni.PrefixLength} {properties.DnsSuffix} {uni.PrefixOrigin} {uni.SuffixOrigin}");
#else
                    Console.WriteLine($"Network {uni.Address.AddressFamily} {uni.Address}/? {properties.DnsSuffix} {uni.PrefixOrigin} {uni.SuffixOrigin}");
#endif

                    switch (uni.Address.AddressFamily)
                    {
                        case AddressFamily.InterNetwork:
                        {
                            var network = new IPNetwork(uni.Address, uni.IPv4Mask);

                            Parallel.ForEach(network.Addresses, (address) =>
                            {
                                var ping = new Ping();
                                var watch = StopWatch.StartNew();
                                var reply = ping.Send(address, 1000);
                                Console.WriteLine($"Ping {new Info(address)} {reply.Status} {TimeSpan.FromMilliseconds(reply.RoundtripTime).FormatTime()} {watch.Elapsed.FormatTime()}");
                            });
                            break;
                        }
                    }
                }
            }

            
        }
      
        void Help()
        {
            Console.WriteLine("subnet-scan [--dns]");
            Console.WriteLine();
            Console.WriteLine("Scans a subnet using icmp");
            Console.WriteLine("--dns:  resolve addresses using dns.");
            Console.WriteLine();
        }
    }
}
