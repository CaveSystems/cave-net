using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Cave.Net;

/// <summary>Provides an async udp client implementation.</summary>
[DebuggerDisplay("{RemoteEndPoint}")]
public class UdpAsyncClient : IDisposable
{
    #region Private Fields

    long bytesReceived;
    long bytesSent;
    bool closing;
    Socket? socket;

    #endregion Private Fields

    #region Private Methods

    void ReceiveCompleted(object? sender, SocketAsyncEventArgs e)
    {
        // taken from Cave.TcpAsyncClient
        if (sender is not Socket socket) return;
        if (e is null) return;
        if (e.Buffer is null) throw new InvalidDataException("Empty buffer!");

        ReadCompletedBegin:
        var bytesTransferred = e.BytesTransferred;

        if (e.RemoteEndPoint is not IPEndPoint remote)
        {
            remote = new IPEndPoint(IPAddress.Any, 0);
        }

        switch (e.SocketError)
        {
            case SocketError.Success:
                break;

            default:
                OnError(remote, new SocketException((int)e.SocketError));
                return;
        }

        try
        {
            Interlocked.Add(ref bytesReceived, bytesTransferred);

            // call event
            OnReceived(remote, e.Buffer, e.Offset, bytesTransferred);

            if (socket.ReceiveTimeout != ReceiveTimeout)
            {
                socket.ReceiveTimeout = ReceiveTimeout;
            }

            // yes read again
            var isPending = socket.ReceiveFromAsync(e);
            if (!isPending)
            {
                goto ReadCompletedBegin;

                // we could do a function call to myself here but with slow OnReceived() functions and fast networks we might get a stack overflow caused by
                // infinite recursion spawning threads using the threadpool is not a good idea either, because multiple receives will mess up our (sequential)
                // stream reading.
            }
            return;
        }
        catch (Exception ex)
        {
            OnError(remote, ex);
        }
        Close();
    }

    #endregion Private Methods

    #region Protected Methods

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        closing = true;
        if (socket != null)
        {
            if (socket is IDisposable disposable)
            {
                disposable.Dispose();
            }
            socket = null;
        }
    }

    /// <summary>Calls the <see cref="Connected"/> event.</summary>
    protected virtual void OnConnect() => Connected?.Invoke(this, new());

    /// <summary>Calls the <see cref="Disconnected"/> event.</summary>
    protected virtual void OnDisconnect() => Disconnected?.Invoke(this, new());

    /// <summary>Calls the <see cref="Error"/> event.</summary>
    /// <param name="remoteEndPoint">The remote endpoint causing the error. This may be null if the host encountered an error.</param>
    /// <param name="ex">The exception (most of the time this will be a <see cref="SocketException"/>.</param>
    protected virtual void OnError(IPEndPoint remoteEndPoint, Exception ex) => Error?.Invoke(this, new(remoteEndPoint, ex));

    /// <summary>Calls the <see cref="Received"/> event.</summary>
    /// <param name="remoteEndPoint">Remote endpoint this message was received from.</param>
    /// <param name="buffer">The buffer containing the received data.</param>
    /// <param name="offset">Byte offset the received data starts.</param>
    /// <param name="length">Length in bytes the of the received data.</param>
    protected virtual void OnReceived(IPEndPoint remoteEndPoint, byte[] buffer, int offset, int length)
    {
        if (Received != null)
        {
            var e = new RemoteEndPointBufferEventArgs(remoteEndPoint, buffer, offset, length);
            Received?.Invoke(this, e);
            if (e.Handled) { }
        }
    }

    #endregion Protected Methods

    #region Public Events

    /// <summary>Event to be called after the connection was established</summary>
    public event EventHandler<EventArgs>? Connected;

    /// <summary>Event to be called after the connection was closed</summary>
    public event EventHandler<EventArgs>? Disconnected;

    /// <summary>Event to be called after an error was encountered</summary>
    public event EventHandler<RemoteEndPointExceptionEventArgs>? Error;

    /// <summary>Event to be called after a buffer was received</summary>
    public event EventHandler<RemoteEndPointBufferEventArgs>? Received;

    #endregion Public Events

    #region Public Properties

    /// <summary>Gets the number of bytes received.</summary>
    public long BytesReceived => Interlocked.Read(ref bytesReceived);

    /// <summary>Gets the number of bytes sent.</summary>
    public long BytesSent => Interlocked.Read(ref bytesSent);

    /// <summary>Gets a value indicating whether the client is bound to a port or not.</summary>
    public bool IsBound => !closing && (socket?.IsBound ?? false);

    /// <summary>Gets the local end point.</summary>
    /// <value>The local end point.</value>
    public IPEndPoint LocalEndPoint { get; private set; } = new IPEndPoint(IPAddress.Any, 0);

    /// <summary>Gets or sets the amount of time, in milliseconds, that a read operation blocks waiting for data.</summary>
    /// <value>
    /// A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value, <see
    /// cref="Timeout.Infinite"/> , specifies that the read operation does not time out.
    /// </value>
    public int ReceiveTimeout { get; set; }

    /// <summary>Gets or sets the amount of time, in milliseconds, that a write operation blocks waiting for transmission.</summary>
    /// <value>
    /// A Int32 that specifies the amount of time, in milliseconds, that will elapse before a write operation fails. The default value, <see
    /// cref="Timeout.Infinite"/> , specifies that the write operation does not time out.
    /// </value>
    public int SendTimeout { get; set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Listens at the specified <paramref name="address"/> and <paramref name="port"/>.</summary>
    /// <param name="address">The ip address to listen at.</param>
    /// <param name="port">The port to listen at.</param>
    /// <exception cref="ObjectDisposedException">UdpAsyncClient.</exception>
    /// <exception cref="InvalidOperationException">Socket is already bound.</exception>
    public void Bind(IPAddress address, int port) => Bind(new IPEndPoint(address, port));

    /// <summary>Listens at the specified end point.</summary>
    /// <param name="endPoint">The end point.</param>
    /// <exception cref="ObjectDisposedException">UdpAsyncClient.</exception>
    /// <exception cref="InvalidOperationException">Socket is already bound.</exception>
    public void Bind(IPEndPoint endPoint)
    {
        if (closing)
        {
            throw new ObjectDisposedException(nameof(UdpAsyncClient));
        }
        if (socket != null)
        {
            throw new InvalidOperationException("Socket is already bound!");
        }

        socket = new(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false
        };
        switch (endPoint.AddressFamily)
        {
            case AddressFamily.InterNetwork:
                break;

            case AddressFamily.InterNetworkV6:
                socket.EnableDualSocket();
                break;
        }
        socket.Bind(endPoint);
        LocalEndPoint = (socket.LocalEndPoint as IPEndPoint) ?? throw new InvalidCastException("Could not cast LocalEndPoint");

        // start receiving
        var e = new SocketAsyncEventArgs
        {
            RemoteEndPoint = LocalEndPoint
        };
        e.Completed += ReceiveCompleted;
        e.SetBuffer(new byte[2048], 0, 2048);
        var isPending = socket.ReceiveFromAsync(e);
        if (!isPending)
        {
            ReceiveCompleted(socket, e);
        }
    }

    /// <summary>Listens at the specified port on IPv4 and IPv6 if available.</summary>
    /// <param name="port">The port.</param>
    /// <exception cref="ObjectDisposedException">UdpAsyncClient.</exception>
    /// <exception cref="InvalidOperationException">Socket is already bound.</exception>
    public void Bind(int port) => Bind(port, null);

    /// <summary>Listens at the specified port.</summary>
    /// <param name="port">The port.</param>
    /// <param name="useIPv6">Use dualstack socket. Defaults value is true.</param>
    /// <exception cref="ObjectDisposedException">UdpAsyncClient.</exception>
    /// <exception cref="InvalidOperationException">Socket is already bound.</exception>
    public void Bind(int port, bool? useIPv6 = null)
    {
        if (closing)
        {
            throw new ObjectDisposedException(nameof(UdpAsyncClient));
        }
        useIPv6 ??= NetworkInterface.GetAllNetworkInterfaces().Any(n => n.GetIPProperties().UnicastAddresses.Any(u => u.Address.AddressFamily == AddressFamily.InterNetworkV6));
        if (useIPv6.GetValueOrDefault(true))
        {
            Bind(new IPEndPoint(IPAddress.IPv6Any, port));
        }
        else
        {
            Bind(new IPEndPoint(IPAddress.Any, port));
        }
    }

    /// <summary>Closes this instance gracefully.</summary>
    public void Close()
    {
        if (closing)
        {
            return;
        }

        closing = true;
        socket?.Close();
        socket = null;
        OnDisconnect();
    }

    /// <summary>Releases unmanaged and managed resources.</summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>Sends a message to the specified remote.</summary>
    /// <param name="remote">Remote address and port to send message to.</param>
    /// <param name="data">An array of type Byte that contains the data to be sent.</param>
    public void SendTo(IPEndPoint remote, byte[] data) => SendTo(remote, data, 0, data.Length);

    /// <summary>Sends a message to the specified remote.</summary>
    /// <param name="remote">Remote address and port to send message to.</param>
    /// <param name="data">An array of type Byte that contains the data to be sent.</param>
    /// <param name="length">The number of bytes to send.</param>
    public void SendTo(IPEndPoint remote, byte[] data, int length) => SendTo(remote, data, 0, length);

    /// <summary>Sends a message to the specified remote.</summary>
    /// <param name="remote">Remote address and port to send message to.</param>
    /// <param name="data">An array of type Byte that contains the data to be sent.</param>
    /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
    /// <param name="length">The number of bytes to send.</param>
    public void SendTo(IPEndPoint remote, byte[] data, int offset, int length)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (remote == null) throw new ArgumentNullException(nameof(remote));
        socket?.SendTo(data, offset, length, SocketFlags.None, remote);
    }

    /// <summary>Sends a message to the specified remote.</summary>
    /// <param name="remote">Remote address and port to send message to.</param>
    /// <param name="data">An array of type Byte that contains the data to be sent.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
    public void SendToAsync<T>(IPEndPoint remote, byte[] data, Action<T?>? callback = null, T? state = default) => SendToAsync(remote, data, 0, data.Length, callback, state);

    /// <summary>Sends a message to the specified remote.</summary>
    /// <param name="remote">Remote address and port to send message to.</param>
    /// <param name="data">An array of type Byte that contains the data to be sent.</param>
    /// <param name="length">The number of bytes to send.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
    public void SendToAsync<T>(IPEndPoint remote, byte[] data, int length, Action<T?>? callback = null, T? state = default) => SendToAsync(remote, data, 0, length, callback, state);

    /// <summary>Sends a message to the specified remote.</summary>
    /// <param name="remote">Remote address and port to send message to.</param>
    /// <param name="data">An array of type Byte that contains the data to be sent.</param>
    /// <param name="offset">The position in the data buffer at which to begin sending data.</param>
    /// <param name="length">The number of bytes to send.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
    public void SendToAsync<T>(IPEndPoint remote, byte[] data, int offset, int length, Action<T?>? callback = null, T? state = default)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));
        if (remote == null) throw new ArgumentNullException(nameof(remote));
        if (socket is null) return;

        void SendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            var remoteEndPoint = e.RemoteEndPoint as IPEndPoint ?? throw new InvalidCastException("Could not cast RemoteEndPoint");
            Interlocked.Add(ref bytesSent, e.BytesTransferred);
            e.Dispose();
            callback?.Invoke(state);
        }

        try
        {
            var e = new SocketAsyncEventArgs { RemoteEndPoint = remote };
            e.Completed += SendCompleted;
            e.SetBuffer(data, offset, length);
            if (socket.SendTimeout != SendTimeout)
            {
                socket.SendTimeout = SendTimeout;
            }
            var isPending = socket.SendToAsync(e);
            if (!isPending)
            {
                SendCompleted(socket, e);
            }
        }
        catch (Exception ex)
        {
            OnError(remote, ex);
            callback?.Invoke(state);
            throw;
        }
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>tcp://localip:port.</returns>
    public override string ToString() => $"udp://{LocalEndPoint}";

    #endregion Public Methods
}
