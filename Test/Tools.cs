using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Test
{
    class Tools
    {
        static int firstPort = 32768 + (Environment.TickCount % 1024);

        public static int GetPort()
        {
            while (true)
            {
                try
                {
                    var port = Interlocked.Increment(ref firstPort);
#pragma warning disable CS0618
                    var listen = new TcpListener(port);
#pragma warning restore CS0618
                    listen.Start();
                    listen.Stop();
                    return port;
                }
                catch
                {
                }
            }
        }

        public static TcpListener OpenPort(out int port)
        {
            while (true)
            {
                try
                {
                    port = Interlocked.Increment(ref firstPort);
#pragma warning disable CS0618
                    var listen = new TcpListener(port);
#pragma warning restore CS0618
                    listen.Start();
                    return listen;
                }
                catch
                {
                }
            }
        }
    }
}
