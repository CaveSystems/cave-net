using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Cave.Net.Dns;

namespace Cave.Net;

/// <summary>Provides a udp server using <see cref="UdpPacket"/> client server communication.</summary>
public class UdpPacketServer
{
    #region Private Fields

    readonly Dictionary<IPEndPoint, UdpPacketClient> clients = new();

    readonly List<Socket> sockets = new();

    volatile bool closed;

    #endregion Private Fields

    #region Private Methods

    void ListenThread_ReadPackets(object? socketObject)
    {
        var socket = (socketObject as Socket) ?? throw new InvalidOperationException("Could not cast socket object!");
        Thread.CurrentThread.IsBackground = true;
        while (!closed)
        {
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                var packet = new UdpPacket
                {
                    LocalEndPoint = (socket.LocalEndPoint as IPEndPoint) ?? throw new InvalidCastException("Could not cast socket endpoint!")
                };
                var bufferSize = socket.Available > 0 ? socket.Available : MaximumPayloadSize;
                packet.Data = new byte[bufferSize];
                packet.Size = (ushort)socket.ReceiveFrom(packet.Data, ref remoteEndPoint);
                packet.RemoteEndPoint = (IPEndPoint)remoteEndPoint;
                Task.Factory.StartNew(p => NewPacket((UdpPacket)p!, socket), packet);
            }
            catch (Exception ex)
            {
                OnException((IPEndPoint)remoteEndPoint, ex);
            }
        }
        socket.Close();
    }

    void NewPacket(UdpPacket packet, Socket socket)
    {
        var newConnection = false;

        // checks all present clients
        lock (clients)
        {
            if (clients.TryGetValue(packet.RemoteEndPoint, out var value))
            {
                value.LastActivity = DateTime.UtcNow;
            }
            else
            {
                newConnection = true;
                clients.Add(packet.RemoteEndPoint, new(packet.RemoteEndPoint, socket));
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
                OnException(packet.RemoteEndPoint, ex);
            }
        }

        // call event
        try
        {
            OnPacketIncoming(packet);
        }
        catch (Exception ex)
        {
            OnException(packet.RemoteEndPoint, ex);
        }
    }

    void TimeoutCheckThread()
    {
        while (!closed)
        {
            Thread.CurrentThread.IsBackground = true;
            var timeoutClients = new List<IPEndPoint>();
            lock (clients)
            {
                foreach (var client in Clients)
                {
                    if ((client.LastActivity + Timeout) > DateTime.UtcNow)
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
                foreach (var client in timeoutClients)
                {
                    try
                    {
                        OnTimeout(client);
                    }
                    catch (Exception ex)
                    {
                        OnException(new IPEndPoint(client.Address, client.Port), ex);
                    }
                }
            }
            else
            {
                Thread.Sleep(1000);
            }
        }
    }

    #endregion Private Methods

    #region Protected Methods

    /// <summary>Will be called whenever an exception occurs in an background thread. If you override this function do not forget to call the base method.</summary>
    /// <param name="remoteEndPoint">Remote endpoint.</param>
    /// <param name="ex">Exception.</param>
    protected virtual void OnException(IPEndPoint remoteEndPoint, Exception ex)
    {
        try
        {
            Error?.Invoke(this, new(remoteEndPoint, ex));
        }
        catch
        {
            // ignore exceptions during exception handling...
        }
    }

    /// <summary>Will be called whenever a new connection was established. If you override this function do not forget to call the base method.</summary>
    /// <param name="remoteEndPoint">Remote endpoint.</param>
    protected virtual void OnNewConnection(IPEndPoint remoteEndPoint)
    {
        try
        {
            Connected?.Invoke(this, new(remoteEndPoint));
        }
        catch (Exception ex)
        {
            OnException(remoteEndPoint, ex);
        }
    }

    /// <summary>Will be called whenever a new packet was received. If you override this function do not forget to call the base method.</summary>
    /// <param name="packet">Incoming packet.</param>
    protected virtual void OnPacketIncoming(UdpPacket packet)
    {
        try
        {
            PacketReceived?.Invoke(this, new(packet));
        }
        catch (Exception ex)
        {
            OnException(packet.RemoteEndPoint, ex);
        }
    }

    /// <summary>Will be called whenever a timeout occured in a background thread. If you override this function do not forget to call the base method.</summary>
    /// <param name="remoteEndPoint">Remote endpoint.</param>
    protected virtual void OnTimeout(IPEndPoint remoteEndPoint)
    {
        try
        {
            TimeoutEvent?.Invoke(this, new(remoteEndPoint));
        }
        catch (Exception ex)
        {
            OnException(remoteEndPoint, ex);
        }
    }

    #endregion Protected Methods

    #region Public Fields

    /// <summary>Obtains the number of bytes a package may contain maximally until it may get fragmented.</summary>
    public const int MaximumPayloadSize = 576 - 40 - 8;

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="UdpPacketServer"/> class.</summary>
    public UdpPacketServer() => new Thread(TimeoutCheckThread).Start();

    #endregion Public Constructors

    #region Public Events

    /// <summary>Event to be called after the connection was established</summary>
    public event EventHandler<RemoteEndPointEventArgs>? Connected;

    /// <summary>Event to be called after an error was encountered</summary>
    public event EventHandler<RemoteEndPointExceptionEventArgs>? Error;

    /// <summary>Event to be called on received packets</summary>
    public event EventHandler<UdpPacketEventArgs>? PacketReceived;

    /// <summary>Provides timeout events</summary>
    public event EventHandler<RemoteEndPointEventArgs>? TimeoutEvent;

    #endregion Public Events

    #region Public Properties

    /// <summary>Gets a list of all clients seen active within <see cref="Timeout"/>.</summary>
    public UdpPacketClient[] Clients
    {
        get
        {
            lock (clients)
            {
                return [.. clients.Values];
            }
        }
    }

    /// <summary>Gets the local <see cref="IPEndPoint"/> s currently connected.</summary>
    public IList<IPEndPoint> LocalEndPoints
    {
        get
        {
            var result = new List<IPEndPoint>();
            lock (sockets)
            {
                foreach (var socket in sockets)
                {
                    if (socket.LocalEndPoint is IPEndPoint ep)
                    {
                        result.Add(ep);
                    }
                }
            }
            return result;
        }
    }

    /// <summary>Gets the remote <see cref="IPEndPoint"/> s currently connected.</summary>
    public IList<IPEndPoint> RemoteEndPoints
    {
        get
        {
            lock (clients)
            {
                return [.. clients.Keys];
            }
        }
    }

    /// <summary>
    /// Gets or sets the timeout <see cref="TimeSpan"/>. Connections without any activity for the specified timeout <see cref="TimeSpan"/> will be considered dead.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes the <see cref="UdpPacketServer"/>.</summary>
    public virtual void Close()
    {
        // do not try to close multiple times
        if (closed)
        {
            return;
        }

        closed = true;
    }

    /// <summary>Starts listening at the specified <see cref="IPEndPoint"/>.</summary>
    /// <param name="ipEndPoint">Ip endpoint to listen at.</param>
    public void Listen(EndPoint ipEndPoint)
    {
        if (ipEndPoint == null)
        {
            throw new ArgumentNullException(nameof(ipEndPoint));
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

    /// <summary>Starts listening at the specified hostname and port.</summary>
    /// <param name="hostName">Hostname to listen at.</param>
    /// <param name="port">Port to listen at.</param>
    public void Listen(string hostName, int port)
    {
        foreach (var address in DnsClient.Default.GetHostAddresses(hostName))
        {
            Listen(address, port);
        }
    }

    /// <summary>Starts listening at the specified ipaddress and port.</summary>
    /// <param name="address">Local address to listen at.</param>
    /// <param name="port">Port to listen at.</param>
    public void Listen(IPAddress address, int port) => Listen(new IPEndPoint(address, port));

    /// <summary>Listens at any ipv6 and ipv4 interfaces at the specified port.</summary>
    /// <param name="port">Port to listen at.</param>
    public void Listen(int port)
    {
        Listen(IPAddress.IPv6Any, port);
        Listen(IPAddress.Any, port);
    }

    /// <summary>Sends a packet via the client connected to the packets destination.</summary>
    /// <param name="packet">Packet to send.</param>
    public virtual void Send(UdpPacket packet)
    {
        if (packet == null)
        {
            throw new ArgumentNullException(nameof(packet));
        }

        lock (clients)
        {
            if (!clients.TryGetValue(packet.RemoteEndPoint, out var client))
            {
                throw new ArgumentException("No client found with the specified remote end point!");
            }
            client.Send(packet);
        }
    }

    /// <summary>Sends a packet via the client connected to the specified destination.</summary>
    /// <param name="destination">Remote endpoint to send data to.</param>
    /// <param name="data">Byte array to send.</param>
    public void Send(IPEndPoint destination, byte[] data) => Send(destination, data, 0, data.Length);

    /// <summary>Sends a packet via the client connected to the specified destination.</summary>
    /// <param name="destination">Remote endpoint to send data to.</param>
    /// <param name="data">Byte array to send.</param>
    /// <param name="size">Number of bytes to send.</param>
    public void Send(IPEndPoint destination, byte[] data, int size) => Send(destination, data, 0, size);

    /// <summary>Sends a packet via the client connected to the specified destination.</summary>
    /// <param name="destination">Remote endpoint to send data to.</param>
    /// <param name="data">Byte array to send.</param>
    /// <param name="offset">Offset at buffer to start sending at.</param>
    /// <param name="size">Number of bytes to send.</param>
    public void Send(IPEndPoint destination, byte[] data, int offset, int size)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        lock (clients)
        {
            if (!clients.TryGetValue(destination, out var client))
            {
                throw new ArgumentException("No client found with the specified remote end point!");
            }
            client.Send(data, offset, size);
        }
    }

    /// <summary>Obtains the string "UcpPacketServer&lt;LocalEndPoint[s]&gt;.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString() => string.Format("UcpPacketServer<{0}>", LocalEndPoints.Join(","));

    #endregion Public Methods
}
