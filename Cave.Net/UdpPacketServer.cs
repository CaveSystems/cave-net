using Cave.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Cave.Net
{
    /// <summary>
    /// Provides a udp server using <see cref="UdpPacket"/> client server communication
    /// </summary>
    public class UdpPacketServer
    {
        /// <summary>
        /// Gets / sets the timeout <see cref="TimeSpan"/>. 
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
        /// If you override this function do not forget to call the base method!
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnTimeout(IPEndPointEventArgs e)
        {
            EventHandler<IPEndPointEventArgs> evt = TimeoutEvent;
            if (evt != null)
            {
                try
                {
                    evt.Invoke(this, e);
                }
                catch (Exception ex)
                {
                    OnException(new ExceptionEventArgs(ex));
                }
            }
        }

        /// <summary>
        /// Will be called whenever a new packet was received.
        /// If you override this function do not forget to call the base method!
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnPacketIncoming(UdpPacketEventArgs e)
        {
            EventHandler<UdpPacketEventArgs> evt = PacketIncomingEvent;
            if (evt != null)
            {
                try
                {
                    evt.Invoke(this, e);
                }
                catch (Exception ex)
                {
                    OnException(new ExceptionEventArgs(ex));
                }
            }
        }

        /// <summary>
        /// Will be called whenever a new connection was established.
        /// If you override this function do not forget to call the base method!
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnNewConnection(IPEndPointEventArgs e)
        {
            EventHandler<IPEndPointEventArgs> evt = NewConnectionEvent;
            if (evt != null)
            {
                try
                {
                    evt.Invoke(this, e);
                }
                catch (Exception ex)
                {
                    OnException(new ExceptionEventArgs(ex));
                }
            }
        }

        /// <summary>
        /// Will be called whenever an exception occurs in an background thread.
        /// If you override this function do not forget to call the base method!
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnException(ExceptionEventArgs e)
        {
            EventHandler<ExceptionEventArgs> evt = ExceptionEvent;
            if (evt != null)
            {
                try
                {
                    evt.Invoke(this, e);
                }
                catch
                {
                    //ignore exceptions during exception handling...
                }
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

        readonly Dictionary<IPEndPoint, UdpPacketClient> m_Clients = new Dictionary<IPEndPoint, UdpPacketClient>();
        readonly List<Socket> m_Sockets = new List<Socket>();

        volatile bool m_Closed = false;

        void ListenThread_ReadPackets(object socketObject)
        {
            Socket socket = (Socket)socketObject;
            Thread.CurrentThread.IsBackground = true;
            while (!m_Closed)
            {
                try
                {
                    UdpPacket packet = new UdpPacket();
                    EndPoint l_EndPoint = socket.LocalEndPoint;
                    int bufferSize = socket.Available > 0 ? socket.Available : MaximumPayloadSize;
                    packet.Data = new byte[bufferSize];
                    packet.Size = (ushort)socket.ReceiveFrom(packet.Data, ref l_EndPoint);
                    packet.RemoteEndPoint = (IPEndPoint)l_EndPoint;
                    packet.ReceivedBy = new UdpPacketClient(packet.RemoteEndPoint, socket);
                    Task.Factory.StartNew((p) => NewPacket((UdpPacket)p), packet);
                }
                catch (Exception ex)
                {
                    OnException(new ExceptionEventArgs(ex));
                }
            }
            socket.Close();
        }

        void TimeoutCheckThread()
        {
            while (!m_Closed)
            {
                Thread.CurrentThread.IsBackground = true;
                List<IPEndPoint> timeoutClients = new List<IPEndPoint>();
                lock (m_Clients)
                {
                    foreach (UdpPacketClient client in Clients)
                    {
                        if (client.LastActivity + Timeout > DateTime.UtcNow)
                        {
                            timeoutClients.Add(client.RemoteEndPoint);
                            if (!m_Clients.Remove(client.RemoteEndPoint))
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
                            OnTimeout(new IPEndPointEventArgs(client));
                        }
                        catch (Exception ex)
                        {
                            OnException(new ExceptionEventArgs(ex));
                        }
                    }
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
        }

        void NewPacket(object packet)
        {
            UdpPacket p = (UdpPacket)packet;
            bool newConnection = false;
            //checl all present clients
            lock (m_Clients)
            {
                if (!m_Clients.ContainsKey(p.RemoteEndPoint))
                {
                    newConnection = true;
                    m_Clients.Add(p.RemoteEndPoint, p.ReceivedBy);
                }
                else
                {
                    m_Clients[p.RemoteEndPoint].LastActivity = DateTime.UtcNow;
                }
            }
            //is new connection
            if (newConnection)
            {
                try
                {
                    OnNewConnection(new IPEndPointEventArgs(p.RemoteEndPoint));
                }
                catch (Exception ex)
                {
                    OnException(new ExceptionEventArgs(ex));
                }
            }
            //call event
            try
            {
                OnPacketIncoming(new UdpPacketEventArgs(p));
            }
            catch (Exception ex)
            {
                OnException(new ExceptionEventArgs(ex));
            }
        }

        #region IPacketServer Member

        /// <summary>
        /// Creates a new <see cref="UdpPacketServer"/>
        /// </summary>
        public UdpPacketServer()
        {
            new Thread(TimeoutCheckThread).Start();
        }

        /// <summary>
        /// Starts listening at the specified <see cref="IPEndPoint"/>
        /// </summary>
        /// <param name="iPEndPoint"></param>
        public void Listen(EndPoint iPEndPoint)
        {
            if (iPEndPoint == null)
            {
                throw new ArgumentNullException("iPEndPoint");
            }

            Socket l_Socket = new Socket(iPEndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            l_Socket.Bind(iPEndPoint);
            l_Socket.Blocking = true;
            lock (m_Sockets)
            {
                m_Sockets.Add(l_Socket);
            }

            new Thread(ListenThread_ReadPackets).Start(l_Socket);
        }

        /// <summary>
        /// Starts listening at the specified hostname and port
        /// </summary>
        /// <param name="hostName"></param>
        /// <param name="port"></param>
        public void Listen(string hostName, int port)
        {
            foreach (IPAddress address in System.Net.Dns.GetHostAddresses(hostName))
            {
                Listen(address, port);
            }
        }

        /// <summary>
        /// Starts listening at the specified ipaddress and port
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        public void Listen(IPAddress address, int port)
        {
            Listen(new IPEndPoint(address, port));
        }

        /// <summary>
        /// Listens at any ipv6 and ipv4 interfaces at the specified port
        /// </summary>
        /// <param name="port"></param>
        public void Listen(int port)
        {
            Listen(IPAddress.IPv6Any, port);
            Listen(IPAddress.Any, port);
        }

        /// <summary>
        /// Obtains the local <see cref="IPEndPoint"/>s currently connected
        /// </summary>
        public IPEndPoint[] LocalEndPoints
        {
            get
            {
                IPEndPoint[] result;
                lock (m_Sockets)
                {
                    result = new IPEndPoint[m_Sockets.Count];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = (IPEndPoint)m_Sockets[i].LocalEndPoint;
                    }
                }
                return result;
            }
        }

        /// <summary>
        /// Obtains the remote <see cref="IPEndPoint"/>s currently connected
        /// </summary>
        public IPEndPoint[] RemoteEndPoints
        {
            get
            {
                lock (m_Clients)
                {
                    return m_Clients.Keys.ToArray();
                }
            }
        }

        /// <summary>
        /// Obtains a list of all clients seen active within <see cref="Timeout"/>
        /// </summary>
        public UdpPacketClient[] Clients
        {
            get
            {
                lock (m_Clients)
                {
                    return m_Clients.Values.ToArray();
                }
            }
        }

        /// <summary>
        /// Sends a packet via the client connected to the packets destination
        /// </summary>
        /// <param name="packet"></param>
        public virtual void Send(UdpPacket packet)
        {
            if (packet == null)
            {
                throw new ArgumentNullException("packet");
            }

            lock (m_Clients)
            {
                if (!m_Clients.ContainsKey(packet.RemoteEndPoint))
                {
                    throw new ArgumentException("No client found with the specified remote end point!");
                }

                UdpPacketClient client = m_Clients[packet.RemoteEndPoint];
                client.Send(packet);
            }
        }

        /// <summary>
        /// Sends a packet via the client connected to the specified destination
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="data"></param>
        public void Send(IPEndPoint destination, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            UdpPacket packet = new UdpPacket
            {
                RemoteEndPoint = destination,
                Data = data,
                Size = (ushort)data.Length
            };
            Send(packet);
        }

        /// <summary>
        /// Obtains the string "UcpPacketServer&lt;LocalEndPoint[s]&gt;
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("UcpPacketServer<{0}>", StringExtensions.Join(LocalEndPoints, ","));
        }

        /// <summary>
        /// Closes the <see cref="UdpPacketServer"/>
        /// </summary>
        public virtual void Close()
        {
            //do not try to close multiple times
            if (m_Closed)
            {
                return;
            }

            m_Closed = true;
        }

        #endregion
    }
}
