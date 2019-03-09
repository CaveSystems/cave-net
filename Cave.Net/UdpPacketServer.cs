using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides a udp server using <see cref="UdpPacket"/> client server communication.
    /// </summary>
    public class UdpPacketServer
    {
        /// <summary>
        /// Gets or sets the timeout <see cref="TimeSpan"/>.
        /// Connections without any activity for the specified timeout <see cref="TimeSpan"/> will be considered dead.
        /// </summary>
        public TimeSpan Timeout { get; set; }

        /// <summary>
        /// Obtains the number of bytes a package may contain maximally until it may get fragmented.
        /// </summary>
        public const int MaximumPayloadSize = 576 - 40 - 8;

        #region Eventhandling

        /// <summary>
        /// Will be called whenever a timeout occured in a background thread.
        /// If you override this function do not forget to call the base method.
        /// </summary>
        /// <param name="remoteEndPoint">Remote endpoint.</param>
        protected virtual void OnTimeout(IPEndPoint remoteEndPoint)
        {
            try
            {
                TimeoutEvent?.Invoke(this, new IPEndPointEventArgs(remoteEndPoint));
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }

        /// <summary>
        /// Will be called whenever a new packet was received.
        /// If you override this function do not forget to call the base method.
        /// </summary>
        /// <param name="packet">Incoming packet.</param>
        protected virtual void OnPacketIncoming(UdpPacket packet)
        {
            try
            {
                PacketIncomingEvent?.Invoke(this, new UdpPacketEventArgs(packet));
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }

        /// <summary>
        /// Will be called whenever a new connection was established.
        /// If you override this function do not forget to call the base method.
        /// </summary>
        /// <param name="remoteEndPoint">Remote endpoint.</param>
        protected virtual void OnNewConnection(IPEndPoint remoteEndPoint)
        {
            try
            {
                NewConnectionEvent?.Invoke(this, new IPEndPointEventArgs(remoteEndPoint));
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }

        /// <summary>
        /// Will be called whenever an exception occurs in an background thread.
        /// If you override this function do not forget to call the base method.
        /// </summary>
        /// <param name="ex">Exception.</param>
        protected virtual void OnException(Exception ex)
        {
            try
            {
                ExceptionEvent?.Invoke(this, new ExceptionEventArgs(ex));
            }
            catch
            {
                // ignore exceptions during exception handling...
            }
        }

        /// <summary>
        /// Provides timeout events
        /// </summary>
        public event EventHandler<IPEndPointEventArgs> TimeoutEvent;

        /// <summary>
        /// Provides packet incoming events
        /// </summary>
        public event EventHandler<UdpPacketEventArgs> PacketIncomingEvent;

        /// <summary>
        /// Provides new connection events
        /// </summary>
        public event EventHandler<IPEndPointEventArgs> NewConnectionEvent;

        /// <summary>
        /// Provides an event for retrieving the exceptions that occure during send / receive
        /// </summary>
        public event EventHandler<ExceptionEventArgs> ExceptionEvent;
        #endregion

        readonly Dictionary<IPEndPoint, UdpPacketClient> clients = new Dictionary<IPEndPoint, UdpPacketClient>();
        readonly List<Socket> sockets = new List<Socket>();
        volatile bool closed = false;

        void ListenThread_ReadPackets(object socketObject)
        {
            var socket = (Socket)socketObject;
            Thread.CurrentThread.IsBackground = true;
            while (!closed)
            {
                try
                {
                    var packet = new UdpPacket();
                    packet.LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    int bufferSize = socket.Available > 0 ? socket.Available : MaximumPayloadSize;
                    packet.Data = new byte[bufferSize];
                    packet.Size = (ushort)socket.ReceiveFrom(packet.Data, ref remoteEndPoint);
                    packet.RemoteEndPoint = (IPEndPoint)remoteEndPoint;
                    Task.Factory.StartNew((p) => NewPacket((UdpPacket)p, socket), packet);
                }
                catch (Exception ex)
                {
                    OnException(ex);
                }
            }
            socket.Close();
        }

        void TimeoutCheckThread()
        {
            while (!closed)
            {
                Thread.CurrentThread.IsBackground = true;
                var timeoutClients = new List<IPEndPoint>();
                lock (clients)
                {
                    foreach (UdpPacketClient client in Clients)
                    {
                        if (client.LastActivity + Timeout > DateTime.UtcNow)
                        {
                            timeoutClients.Add(client.RemoteEndPoint);
                            if (!clients.Remove(client.RemoteEndPoint))
                            {
                                throw new KeyNotFoundException();
                            }

                            client.Close();
                        }
                    }
                }
                if (timeoutClients.Count > 0)
                {
                    foreach (IPEndPoint client in timeoutClients)
                    {
                        try
                        {
                            OnTimeout(client);
                        }
                        catch (Exception ex)
                        {
                            OnException(ex);
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        void NewPacket(UdpPacket packet, Socket socket)
        {
            bool newConnection = false;

            // checl all present clients
            lock (clients)
            {
                if (!clients.ContainsKey(packet.RemoteEndPoint))
                {
                    newConnection = true;
                    clients.Add(packet.RemoteEndPoint, new UdpPacketClient(packet.RemoteEndPoint, socket));
                }
                else
                {
                    clients[packet.RemoteEndPoint].LastActivity = DateTime.UtcNow;
                }
            }

            // is new connection
            if (newConnection)
            {
                try
                {
                    OnNewConnection(packet.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    OnException(ex);
                }
            }

            // call event
            try
            {
                OnPacketIncoming(packet);
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }

        #region IPacketServer Member

        /// <summary>
        /// Initializes a new instance of the <see cref="UdpPacketServer"/> class.
        /// </summary>
        public UdpPacketServer()
        {
            new Thread(TimeoutCheckThread).Start();
        }

        /// <summary>
        /// Starts listening at the specified <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="ipEndPoint">Ip endpoint to listen at.</param>
        public void Listen(EndPoint ipEndPoint)
        {
            if (ipEndPoint == null)
            {
                throw new ArgumentNullException("iPEndPoint");
            }

            var socket = new Socket(ipEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(ipEndPoint);
            socket.Blocking = true;
            lock (sockets)
            {
                sockets.Add(socket);
            }

            new Thread(ListenThread_ReadPackets).Start(socket);
        }

        /// <summary>
        /// Starts listening at the specified hostname and port.
        /// </summary>
        /// <param name="hostName">Hostname to listen at.</param>
        /// <param name="port">Port to listen at.</param>
        public void Listen(string hostName, int port)
        {
            foreach (IPAddress address in System.Net.Dns.GetHostAddresses(hostName))
            {
                Listen(address, port);
            }
        }

        /// <summary>
        /// Starts listening at the specified ipaddress and port.
        /// </summary>
        /// <param name="address">Local address to listen at.</param>
        /// <param name="port">Port to listen at.</param>
        public void Listen(IPAddress address, int port)
        {
            Listen(new IPEndPoint(address, port));
        }

        /// <summary>
        /// Listens at any ipv6 and ipv4 interfaces at the specified port.
        /// </summary>
        /// <param name="port">Port to listen at.</param>
        public void Listen(int port)
        {
            Listen(IPAddress.IPv6Any, port);
            Listen(IPAddress.Any, port);
        }

        /// <summary>
        /// Gets the local <see cref="IPEndPoint"/>s currently connected.
        /// </summary>
        public IPEndPoint[] LocalEndPoints
        {
            get
            {
                IPEndPoint[] result;
                lock (sockets)
                {
                    result = new IPEndPoint[sockets.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = (IPEndPoint)sockets[i].LocalEndPoint;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the remote <see cref="IPEndPoint"/>s currently connected.
        /// </summary>
        public IPEndPoint[] RemoteEndPoints
        {
            get
            {
                lock (clients)
                {
                    return clients.Keys.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets a list of all clients seen active within <see cref="Timeout"/>.
        /// </summary>
        public UdpPacketClient[] Clients
        {
            get
            {
                lock (clients)
                {
                    return clients.Values.ToArray();
                }
            }
        }

        /// <summary>
        /// Sends a packet via the client connected to the packets destination.
        /// </summary>
        /// <param name="packet">Packet to send.</param>
        public virtual void Send(UdpPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            lock (clients)
            {
                if (!clients.ContainsKey(packet.RemoteEndPoint))
                {
                    throw new ArgumentException("No client found with the specified remote end point!");
                }

                UdpPacketClient client = clients[packet.RemoteEndPoint];
                client.Send(packet);
            }
        }

        /// <summary>
        /// Sends a packet via the client connected to the specified destination.
        /// </summary>
        /// <param name="destination">Remote endpoint to send data to.</param>
        /// <param name="data">Byte array to send.</param>
        public void Send(IPEndPoint destination, byte[] data) => Send(destination, data, 0, data.Length);

        /// <summary>
        /// Sends a packet via the client connected to the specified destination.
        /// </summary>
        /// <param name="destination">Remote endpoint to send data to.</param>
        /// <param name="data">Byte array to send.</param>
        /// <param name="size">Number of bytes to send.</param>
        public void Send(IPEndPoint destination, byte[] data, int size) => Send(destination, data, 0, size);

        /// <summary>
        /// Sends a packet via the client connected to the specified destination.
        /// </summary>
        /// <param name="destination">Remote endpoint to send data to.</param>
        /// <param name="data">Byte array to send.</param>
        /// <param name="offset">Offset at buffer to start sending at.</param>
        /// <param name="size">Number of bytes to send.</param>
        public void Send(IPEndPoint destination, byte[] data, int offset, int size)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            lock (clients)
            {
                if (!clients.ContainsKey(destination))
                {
                    throw new ArgumentException("No client found with the specified remote end point!");
                }

                UdpPacketClient client = clients[destination];
                client.Send(data, offset, size);
            }
        }

        /// <summary>
        /// Obtains the string "UcpPacketServer&lt;LocalEndPoint[s]&gt;.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return string.Format("UcpPacketServer<{0}>", StringExtensions.Join(LocalEndPoints, ","));
        }

        /// <summary>
        /// Closes the <see cref="UdpPacketServer"/>.
        /// </summary>
        public virtual void Close()
        {
            // do not try to close multiple times
            if (closed)
            {
                return;
            }

            closed = true;
        }

        #endregion
    }
}
