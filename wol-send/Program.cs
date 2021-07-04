using Cave;
using Cave.Net;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Net.NetworkInformation;

namespace wol
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                new Program().Run(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        void Run(string[] args)
        {
            if (args.Length == 0)
            {
                Help();
                return;
            }

            string password = args.FirstOrDefault(a => a.StartsWith("--pwd"))?.AfterFirst("--pwd=");
            foreach (var target in args.Where(a => !a.StartsWith("-")))
            {
                PhysicalAddress address;
                try
                {
                    address = PhysicalAddress.Parse(target.Replace(":", "").Replace("-", ""));
                }
                catch
                {
                    throw new Exception($"Invalid physical address: {target}!\nExpected format: 00-11-22-33-44-55 or 00:11:22:33:44:55");
                }

                var result = WakeOnLan.SendMagicPacket(address, password);
                foreach (var item in result)
                {
                    if (item.Value != null)
                    {
                        Console.WriteLine($"WakeOnLan {item.Key}: {item.Value.Message}");
                    }
                    else
                    {
                        Console.WriteLine($"WakeOnLan {item.Key}: sent");
                    }
                }
            }
        }

        void Help()
        {
            Console.WriteLine("wol-send <mac> [<mac> [..]] [--pwd=password]");
            Console.WriteLine();
            Console.WriteLine("Sends a wake on lan (magic packet) via network broadcast to the specified mac addesses");
            Console.WriteLine("using an optional SecureOnPassword.");
            Console.WriteLine();
        }
    }
}
