using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Test
{
    class Tools
    {
        static int firstPort = 10000 + (Environment.TickCount % 10000) + (Thread.CurrentThread.ManagedThreadId.GetHashCode() % 10000);

        public static int GetPort()
        {
            while (true)
            {
                try
                {
                    var port = Interlocked.Increment(ref firstPort);
                    var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        LingerState = new LingerOption(false, 0),
                        ExclusiveAddressUse = true
                    };
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
                    sock.Bind(new IPEndPoint(IPAddress.Any, port));
                    sock.Close();
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
