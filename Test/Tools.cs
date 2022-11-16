using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Test
{
    class Tools
    {
        #region Private Fields

        static int currentPort;

        #endregion Private Fields

        #region Public Methods

        public static int GetPort()
        {
            while (true)
            {
                try
                {
                    var port = 10000 + (Interlocked.Increment(ref currentPort) % 10000);
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
                    port = 10000 + (Interlocked.Increment(ref currentPort) % 10000);
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

        #endregion Public Methods
    }
}
