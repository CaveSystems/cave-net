using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Cave.Net.Ntp
{
    /// <summary>
    /// Provides a simple udp server socket.
    /// </summary>
    public class UdpServer : IDisposable
    {
        const SocketOptionName ipv6only = (SocketOptionName)27;

        Socket socket;
        byte[] buffer = new byte[2048];
        EndPoint client = new IPEndPoint(0, 0);

        void OnReceived(IAsyncResult ar)
        {
            if (socket == null)
            {
                return;
            }

            int length = socket.EndReceiveFrom(ar, ref client);
            byte[] copy = new byte[length];
            Buffer.BlockCopy(buffer, 0, copy, 0, length);
            var packet = new UdpPacket()
            {
                Data = copy,
                Size = (ushort)length,
                RemoteEndPoint = (IPEndPoint)client,
            };
            if (AsyncCallback)
            {
                void CallOnReceived(object p) => OnReceived((UdpPacket)p);
                Task.Factory.StartNew(CallOnReceived, packet);
            }
            else
            {
                OnReceived(packet);
            }

            socket?.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref client, OnReceived, null);
        }

        /// <summary>
        /// Calls the <see cref="Received"/> event.
        /// </summary>
        /// <param name="packet">The received udp packet.</param>
        protected virtual void OnReceived(UdpPacket packet)
        {
            Received?.Invoke(this, new UdpPacketEventArgs(packet));
        }

        /// <summary>
        /// Calls the <see cref="Sent"/> event after the package has been sent.
        /// </summary>
        /// <param name="packet">The sent udp packet.</param>
        protected virtual void OnSent(UdpPacket packet)
        {
            Sent?.Invoke(this, new UdpPacketEventArgs(packet));
        }

        /// <summary>
        /// Gets or sets a value indicating whether async callbacks are used for received packets.
        /// </summary>
        public bool AsyncCallback { get; set; }

        /// <summary>
        /// Listens for incoming packages at the specified port.
        /// </summary>
        /// <param name="port">The port number to listen at.</param>
        public void Listen(int port)
        {
            if (socket != null)
            {
                throw new InvalidOperationException("Socket already opened!");
            }

            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            socket.SetSocketOption(SocketOptionLevel.IPv6, ipv6only, false);
            socket.Bind(new IPEndPoint(IPAddress.IPv6Any, 123));
            socket.BeginReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref client, OnReceived, null);
        }

        /// <summary>
        /// Sends a packet to the specified endpoint.
        /// </summary>
        /// <param name="packet">Packet to send.</param>
        public void Send(UdpPacket packet)
        {
            socket.SendTo(packet.Data, packet.Size, SocketFlags.None, packet.RemoteEndPoint);
            OnSent(packet);
        }

        /// <summary>
        /// Closes the socket.
        /// </summary>
        public void Close()
        {
            socket?.Close();
            Dispose();
        }

        /// <summary>
        /// Releases all resources.
        /// </summary>
        public void Dispose()
        {
            if (socket is IDisposable disposable)
            {
                disposable.Dispose();
            }
            socket = null;
            buffer = null;
            client = null;
        }

        /// <summary>
        /// Provides an event for each received package.
        /// </summary>
        public event EventHandler<UdpPacketEventArgs> Received;

        /// <summary>
        /// Provides an event for each sent package.
        /// </summary>
        public event EventHandler<UdpPacketEventArgs> Sent;
    }
}
