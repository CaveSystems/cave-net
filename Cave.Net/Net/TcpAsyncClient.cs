using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cave.IO;
using Cave.Net.Dns;

namespace Cave.Net;

/// <summary>Provides an async tcp client implementation.</summary>
[DebuggerDisplay("{RemoteEndPoint}")]
public class TcpAsyncClient : IDisposable
{
    #region Private Fields

    readonly object syncRoot = new();
    long bytesReceived;
    long bytesSent;
    volatile bool closing;
    bool connectedEventTriggered;
    bool disconnectedEventTriggered;
    bool disposed;
    LingerOption lingerState = new(false, 0);
    bool noDelay;
    int pendingAsyncSends;
    int receiveTimeout;
    int sendTimeout;
    SocketAsyncEventArgs? socketAsync;
    short ttl = 255;
    Socket? uncheckedSocket;

    #endregion Private Fields

    #region Private Properties

    Socket CheckedSocket => uncheckedSocket == null
        ? throw new InvalidOperationException("Not connected!")
        : closing
            ? throw new ObjectDisposedException(nameof(TcpAsyncClient))
            : uncheckedSocket;

    bool Initialized => uncheckedSocket != null;

    #endregion Private Properties

    #region Private Methods

    T CachedValue<T>(ref T field, Func<T?> func)
        where T : struct
    {
        if (!closing)
        {
            var value = func();
            if (value is not null) field = value.Value;
        }
        return field;
    }

    T CachedValue<T>(ref T field, Func<T?> func)
        where T : class
    {
        if (!closing)
        {
            var value = func();
            if (value is not null) field = value;
        }
        return field;
    }

    void ConnectAsyncCallback(object? sender, SocketAsyncEventArgs e)
    {
        EnterLock();
        try
        {
            if (e.SocketError != SocketError.Success)
            {
#if !NET20 && !NET35
                if (e.ConnectByNameError != null)
                {
                    OnError(e.ConnectByNameError);
                    return;
                }
#endif
                OnError(new SocketException((int)e.SocketError));
                return;
            }
            var socket = (sender as Socket) ?? throw new InvalidCastException("Could not cast sender to socket!");
            if (!socket.Connected)
            {
                OnError(new SocketException((int)SocketError.SocketError));
                return;
            }
            try
            {
                InitializeSocket(socket);
                StartReader();
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }
        finally
        {
            e.Dispose();
            ExitLock();
        }
    }

    void ConnectAsyncCallback(IAsyncResult asyncResult)
    {
        EnterLock();
        try
        {
            var socket = (asyncResult.AsyncState as Socket) ?? throw new InvalidOperationException("Invalid AsyncState.");
            if (!socket.Connected)
            {
                OnError(new SocketException((int)SocketError.SocketError));
                return;
            }
            socket.EndConnect(asyncResult);
            InitializeSocket(socket);
            StartReader();
        }
        catch (Exception ex)
        {
            OnError(ex);
        }
        finally
        {
            ExitLock();
        }
    }

    void ConnectInternal(IPEndPoint endpoint)
    {
        EnterLock();
        try
        {
            while (true)
            {
                var socket = CreateSocket(endpoint.AddressFamily);
                var asyncResult = socket.BeginConnect(endpoint.Address, endpoint.Port, null, null);
                if (HandleAsyncConnectResult(socket, asyncResult))
                {
                    return;
                }
            }
        }
        finally
        {
            ExitLock();
        }
    }

    void ConnectInternal(string hostname, int port)
    {
        EnterLock();
        try
        {
            var response = DnsClient.Default.Resolve(hostname);
            var errors = new List<Exception>();
            foreach (var answer in response.Answers)
            {
                if (IPAddress.TryParse($"{answer.Value}", out var address))
                {
                    try
                    {
                        ConnectInternal(new(address, port));
                        return;
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }
            }
            throw new AggregateException($"Could not connect to any ip address for host {hostname}!", errors);
        }
        finally
        {
            ExitLock();
        }
    }

    Socket CreateSocket(AddressFamily family)
    {
        if (closing)
        {
            throw new ObjectDisposedException(nameof(TcpAsyncClient));
        }
#if NET20 || NET35 || NET40
        var socket = new Socket(family, SocketType.Stream, ProtocolType.Tcp);
#else
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
#endif
        socket.ExclusiveAddressUse = false;
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        SetSocketOptions(socket);
        return socket;
    }

    void DisposeUnmanaged()
    {
        if (!disposed)
        {
            disposed = true;
            {
                if (uncheckedSocket is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            {
                if (socketAsync is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }

    void EnterLock()
    {
        if (!Monitor.TryEnter(syncRoot, DeadLockTimeout))
        {
            throw new TimeoutException($"DeadLock timeout exceeded. This can be caused by inproper use of {nameof(TcpAsyncClient)} events!");
        }
    }

    void ExitLock() => Monitor.Exit(syncRoot);

    bool HandleAsyncConnectResult(Socket socket, IAsyncResult asyncResult)
    {
        try
        {
            if (asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout))
            {
                socket.EndConnect(asyncResult);
                InitializeSocket(socket);
                StartReader();
                return true;
            }
            Close();
            throw new TimeoutException();
        }
        catch (SocketException s)
        {
            if (s.SocketErrorCode != SocketError.AddressAlreadyInUse)
            {
                OnError(s);
                throw;
            }
            return false;
        }
        catch (Exception ex)
        {
            OnError(ex);
            throw;
        }
    }

    void InternalSendAsync<T>(byte[] buffer, int offset, int length, Action<T> callback, T state)
    {
        void Callback() => callback?.Invoke(state);
        InternalSendAsync(buffer, offset, length, Callback);
    }

    void InternalSendAsync(byte[] buffer, int offset, int length, Action? callback = null)
    {
        void Completed(object? sender, SocketAsyncEventArgs e)
        {
            if (e is null) return;
            if (e.Buffer is null) throw new InvalidDataException("Empty buffer!");
            Interlocked.Decrement(ref pendingAsyncSends);
            Interlocked.Add(ref bytesSent, e.BytesTransferred);
            OnSent(e.Buffer, e.Offset, e.BytesTransferred);
            if (e.SocketError != SocketError.Success)
            {
                OnError(new SocketException((int)e.SocketError));
                Close();
            }
            e.Dispose();
            callback?.Invoke();
        }

        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected!");
        }

        EnterLock();
        try
        {
            Interlocked.Increment(ref pendingAsyncSends);
            var args = new SocketAsyncEventArgs();
            args.SetBuffer(buffer, offset, length);
            args.Completed += Completed;
            var isPending = CheckedSocket.SendAsync(args);
            if (!isPending)
            {
                Completed(CheckedSocket, args);
            }
        }
        catch (Exception ex)
        {
            Interlocked.Decrement(ref pendingAsyncSends);
            OnError(ex);
            Close();
            callback?.Invoke();
            throw;
        }
        finally
        {
            ExitLock();
        }
    }

    /// <summary>Gets called whenever a read is completed.</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="e">The <see cref="SocketAsyncEventArgs"/> instance containing the event data.</param>
    void ReadCompleted(object? sender, SocketAsyncEventArgs e)
    {
    ReadCompletedBegin:

        if (e is null || socketAsync is null) return;
        switch (e.SocketError)
        {
            case SocketError.Success:
                break;

            case SocketError.ConnectionReset:
                if (!closing)
                {
                    Close();
                }
                return;

            default:
                OnError(new SocketException((int)e.SocketError));
                goto case SocketError.ConnectionReset;
        }

        var bytesTransferred = e.BytesTransferred;
        try
        {
            if (e.Buffer is null) throw new InvalidDataException("Empty buffer!");

            // got data (if not this is the disconnected call)
            if (bytesTransferred > 0)
            {
                Interlocked.Add(ref bytesReceived, bytesTransferred);

                // call event
                OnReceived(e.Buffer, e.Offset, bytesTransferred, out var handled);
                if (!handled)
                {
                    // cleanup read buffers and add new data
                    lock (ReceiveBuffer)
                    {
                        ReceiveBuffer.FreeBuffers();
                        ReceiveBuffer.AppendBuffer(e.Buffer, e.Offset, bytesTransferred);
                        OnBuffered();
                        Monitor.PulseAll(ReceiveBuffer);
                    }
                }

                if (closing)
                {
                    return;
                }

                // read next
                var isPending = CheckedSocket.ReceiveAsync(socketAsync);
                if (!isPending)
                {
                    e = socketAsync;
                    goto ReadCompletedBegin;

                    // we could do a function call to myself here but with slow OnReceived() functions and fast networks we might get a stack overflow caused by
                    // infinite recursion spawning threads using the threadpool is not a good idea either, because multiple receives will mess up our
                    // (sequential) stream reading.
                }
                return;
            }
        }
        catch (Exception ex)
        {
            if (!closing)
            {
                OnError(ex);
            }
        }
        if (!closing)
        {
            Close();
        }
    }

    void SetSocketOptions(Socket socket)
    {
        socket.NoDelay = noDelay;
        socket.Ttl = ttl;
        socket.LingerState = lingerState;
        if (!lingerState.Enabled)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
        }
        socket.SendTimeout = sendTimeout;
        socket.ReceiveTimeout = receiveTimeout;
    }

    void SetValue<T>(ref T field, Action<T> setter, T value)
    {
        if ((uncheckedSocket != null) && !closing)
        {
            setter(value);
        }
        field = value;
    }

    #endregion Private Methods

    #region Protected Methods

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!closing)
            {
                Close();
            }
        }
        DisposeUnmanaged();
    }

    /// <summary>Calls the <see cref="Buffered"/> event (if set).</summary>
    protected virtual void OnBuffered() => Buffered?.Invoke(this, new());

    /// <summary>Calls the <see cref="Connected"/> event (if set).</summary>
    protected virtual void OnConnect()
    {
        if (connectedEventTriggered)
        {
            throw new InvalidOperationException("OnConnect triggered twice!");
        }

        connectedEventTriggered = true;
        Connected?.Invoke(this, new());
    }

    /// <summary>Calls the <see cref="Disconnected"/> event (if set).</summary>
    protected virtual void OnDisconnect()
    {
        if (connectedEventTriggered && !disconnectedEventTriggered)
        {
            disconnectedEventTriggered = true;
            Disconnected?.Invoke(this, new());
        }
    }

    /// <summary>Calls the <see cref="Received"/> event (if set).</summary>
    /// <remarks>
    /// You can set <see cref="BufferEventArgs.Handled"/> to true when overriding this function or within <see cref="Received"/> to skip adding data to the
    /// <see cref="Stream"/> and <see cref="ReceiveBuffer"/>.
    /// </remarks>
    /// <param name="buffer">Receive buffer instance.</param>
    /// <param name="offset">Start offset of the received data.</param>
    /// <param name="length">Length in bytes of the received data.</param>
    /// <param name="handled">If set the data was already handled and will not be buffered.</param>
    protected virtual void OnReceived(byte[] buffer, int offset, int length, out bool handled)
    {
        if (Received != null)
        {
            var e = new BufferEventArgs(buffer, offset, length);
            Received?.Invoke(this, e);
            handled = e.Handled;
        }
        else
        {
            handled = false;
        }
    }

    /// <summary>Calls the <see cref="Sent"/> event (if set).</summary>
    /// <param name="buffer">Sent buffer instance.</param>
    /// <param name="offset">Start offset of the sent data.</param>
    /// <param name="length">Length in bytes of the sent data.</param>
    protected virtual void OnSent(byte[] buffer, int offset, int length) => Sent?.Invoke(this, new(buffer, offset, length));

    #endregion Protected Methods

    #region Protected Internal Methods

    /// <summary>Initializes the client for use with the specified <paramref name="server"/> instance.</summary>
    /// <exception cref="InvalidOperationException">Reader already started.</exception>
    /// <param name="server">Server instance this client belongs to.</param>
    /// <param name="socket">Socket instance this client uses.</param>
    protected internal virtual void InitializeServer(ITcpServer server, Socket socket)
    {
        if (socket == null)
        {
            throw new ArgumentNullException(nameof(socket));
        }
        Server = server ?? throw new ArgumentNullException(nameof(server));
        BufferSize = server.BufferSize;
        ReceiveTimeout = server.ReceiveTimeout;
        SendTimeout = server.SendTimeout;
        InitializeSocket(socket);
    }

    /// <summary>Calls the Error event (if set) and closes the connection.</summary>
    /// <param name="ex">The exception (most of the time this will be a <see cref="SocketException"/>.</param>
    protected internal virtual void OnError(Exception ex)
    {
        if (!closing)
        {
            Error?.Invoke(this, new(ex));
            Close();
        }
    }

    #endregion Protected Internal Methods

    #region Internal Methods

    internal void InitializeSocket(Socket socket)
    {
        if (socket == null)
        {
            throw new ArgumentNullException(nameof(socket));
        }
        if (Initialized)
        {
            throw new InvalidOperationException("Already initialized!");
        }
        SetSocketOptions(socket);
        RemoteEndPoint = (socket.RemoteEndPoint as IPEndPoint) ?? throw new InvalidOperationException("Could not get RemoteEndPoint!");
        LocalEndPoint = (socket.LocalEndPoint as IPEndPoint) ?? throw new InvalidOperationException("Could not get LocalEndPoint!");
        uncheckedSocket = socket;
    }

    /// <summary>Calls the <see cref="OnConnect()"/> function and starts the async socket reader.</summary>
    internal void StartReader()
    {
        if (socketAsync != null)
        {
            throw new InvalidOperationException("Reader already started!");
        }
        OnConnect();
        CheckedSocket.SendBufferSize = BufferSize;
        CheckedSocket.ReceiveBufferSize = BufferSize;
        var buffer = new byte[BufferSize];
        socketAsync = new()
        { UserToken = this };
        socketAsync.Completed += ReadCompleted;
        socketAsync.SetBuffer(buffer, 0, buffer.Length);
        var isPending = CheckedSocket.ReceiveAsync(socketAsync);
        if (!isPending)
        {
            ThreadPool.QueueUserWorkItem(e => ReadCompleted(this, (SocketAsyncEventArgs)e!), socketAsync);
        }
    }

    #endregion Internal Methods

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="TcpAsyncClient"/> class.</summary>
    public TcpAsyncClient()
    {
        Stream = new(this);
        LocalEndPoint = new IPEndPoint(IPAddress.Any, 0);
        RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
    }

    #endregion Public Constructors

    #region Public Events

    /// <summary>Event to be called after a buffer was received and was not handled by the <see cref="Received"/> event</summary>
    public event EventHandler<EventArgs>? Buffered;

    /// <summary>Event to be called after the connection was established</summary>
    public event EventHandler<EventArgs>? Connected;

    /// <summary>Event to be called after the connection was closed</summary>
    public event EventHandler<EventArgs>? Disconnected;

    /// <summary>Event to be called after an error was encountered</summary>
    public event EventHandler<ExceptionEventArgs>? Error;

    /// <summary>Event to be called after a buffer was received</summary>
    public event EventHandler<BufferEventArgs>? Received;

    /// <summary>Event to be called after a buffer was sent</summary>
    public event EventHandler<BufferEventArgs>? Sent;

    #endregion Public Events

    #region Public Properties

    /// <summary>Gets the buffer size used.</summary>
    public int BufferSize { get; private set; }

    /// <summary>Gets the number of bytes received.</summary>
    public long BytesReceived => Interlocked.Read(ref bytesReceived);

    /// <summary>Gets the number of bytes sent.</summary>
    public long BytesSent => Interlocked.Read(ref bytesSent);

    /// <summary>Gets or sets the amount of time, in milliseconds, that a connect operation blocks waiting for data.</summary>
    /// <value>
    /// A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value,
    /// <see cref="Timeout.Infinite"/> , specifies that the connect operation does not time out.
    /// </value>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>Gets or sets the dead lock timeout. This is the maximum time thread safe functions wait for acquiring the socket lock.</summary>
    public TimeSpan DeadLockTimeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets a value indicating whether the client is connected.</summary>
    public bool IsConnected => !closing && (uncheckedSocket?.Connected ?? false);

    /// <summary>Gets or sets a value that specifies whether the Socket will delay closing a socket in an attempt to send all pending data.</summary>
    /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
    public LingerOption LingerState
    {
        get => CachedValue(ref lingerState, () => uncheckedSocket?.LingerState);
        set => SetValue(ref lingerState, v => CheckedSocket.LingerState = v, value);
    }

    /// <summary>Gets the local end point.</summary>
    /// <value>The local end point.</value>
    public IPEndPoint LocalEndPoint { get; private set; }

    /// <summary>Gets or sets a value indicating whether the stream Socket is using the Nagle algorithm.</summary>
    /// <value><c>true</c> if the Socket uses the Nagle algorithm; otherwise, <c>false</c>.</value>
    /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
    public bool NoDelay
    {
        get => CachedValue(ref noDelay, () => uncheckedSocket?.NoDelay);
        set => SetValue(ref noDelay, v => CheckedSocket.NoDelay = v, value);
    }

    /// <summary>Gets the number of active async send tasks.</summary>
    public int PendingAsyncSends => pendingAsyncSends;

    /// <summary>Gets the receive buffer.</summary>
    /// <value>The receive buffer.</value>
    /// <remarks>
    /// Use lock on this buffer to ensure thread safety when using concurrent access to the <see cref="Stream"/> property, <see cref="GetStream()"/> function
    /// and/or <see cref="Received"/> callbacks.
    /// </remarks>
    public FifoStream ReceiveBuffer { get; } = new();

    /// <summary>Gets or sets the amount of time, in milliseconds, that a read operation blocks waiting for data.</summary>
    /// <value>
    /// A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value,
    /// <see cref="Timeout.Infinite"/> , specifies that the read operation does not time out.
    /// </value>
    /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
    public int ReceiveTimeout
    {
        get => CachedValue(ref receiveTimeout, () => uncheckedSocket?.ReceiveTimeout);
        set => SetValue(ref receiveTimeout, v => CheckedSocket.ReceiveTimeout = v, value);
    }

    /// <summary>Gets the remote end point.</summary>
    /// <value>The remote end point.</value>
    public IPEndPoint RemoteEndPoint { get; private set; }

    /// <summary>Gets or sets the amount of time, in milliseconds, that a write operation blocks waiting for transmission.</summary>
    /// <value>
    /// A Int32 that specifies the amount of time, in milliseconds, that will elapse before a write operation fails. The default value,
    /// <see cref="Timeout.Infinite"/> , specifies that the write operation does not time out.
    /// </value>
    /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
    public int SendTimeout
    {
        get => CachedValue<int>(ref sendTimeout, () => uncheckedSocket?.SendTimeout);
        set => SetValue(ref sendTimeout, v => CheckedSocket.SendTimeout = v, value);
    }

    /// <summary>Gets the server instance this client belongs to. May be <c>null</c>.</summary>
    public ITcpServer? Server { get; private set; }

    /// <summary>Gets or sets an user defined object.</summary>
    public object? State { get; set; }

    /// <summary>Gets the raw TCP stream used to send and receive data.</summary>
    /// <remarks>This function and access to all stream functions are threadsafe.</remarks>
    /// <value>The TCP stream instance.</value>
    public TcpAsyncStream Stream { get; }

    /// <summary>Gets or sets a value that specifies the Time To Live (TTL) value of Internet Protocol (IP) packets sent by the Socket.</summary>
    /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
    public short Ttl
    {
        get => CachedValue(ref ttl, () => uncheckedSocket?.Ttl);
        set => SetValue(ref ttl, v => CheckedSocket.Ttl = v, value);
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes this instance gracefully.</summary>
    /// <remarks>
    /// To ensure unsent data is flushed when using <see cref="Stream"/> use the <see cref="TcpAsyncStream.Close"/> method or <see cref="TcpAsyncStream.Flush"/> first.
    /// <para>This function is threadsafe.</para>
    /// </remarks>
    public virtual void Close()
    {
        EnterLock();
        try
        {
            if (closing)
            {
                return;
            }

            closing = true;
            OnDisconnect();

            if (uncheckedSocket?.Connected ?? false)
            {
                try
                {
                    uncheckedSocket.Shutdown(SocketShutdown.Both);
                    uncheckedSocket.Close();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            }
        }
        finally
        {
            ExitLock();
        }
        DisposeUnmanaged();
    }

    /// <summary>Connects to the specified hostname and port.</summary>
    /// <param name="hostname">hostname to resolve.</param>
    /// <param name="port">port to connect to.</param>
    /// <param name="bufferSize">tcp buffer size in bytes.</param>
    public void Connect(string hostname, int port, int bufferSize = 64 * 1024)
    {
        if (IPAddress.TryParse(hostname, out var ipaddress))
        {
            Connect(ipaddress, port, bufferSize);
            return;
        }

        if (closing)
        {
            throw new ObjectDisposedException(nameof(TcpAsyncClient));
        }

        if (Initialized)
        {
            throw new InvalidOperationException("Client already connected!");
        }

        BufferSize = bufferSize;
        ConnectInternal(hostname, port);
    }

    /// <summary>Connects to the specified address and port.</summary>
    /// <param name="address">ip address to connect to.</param>
    /// <param name="port">port to connect to.</param>
    /// <param name="bufferSize">tcp buffer size in bytes.</param>
    public void Connect(IPAddress address, int port, int bufferSize = 64 * 1024)
    {
        if (closing)
        {
            throw new ObjectDisposedException(nameof(TcpAsyncClient));
        }

        if (Initialized)
        {
            throw new InvalidOperationException("Client already connected!");
        }

        RemoteEndPoint = new(address, port);
        BufferSize = bufferSize;
        ConnectInternal(new(address, port));
    }

    /// <summary>Connects to the specified address and port.</summary>
    /// <param name="endPoint">ip endpoint to connect to.</param>
    /// <param name="bufferSize">tcp buffer size in bytes.</param>
    public void Connect(IPEndPoint endPoint, int bufferSize = 64 * 1024) => Connect(endPoint.Address, endPoint.Port, bufferSize);

    /// <summary>Connects to the specified hostname and port.</summary>
    /// <param name="hostname">hostname to resolve.</param>
    /// <param name="port">port to connect to.</param>
    /// <param name="bufferSize">tcp buffer size in bytes.</param>
    public void ConnectAsync(string hostname, int port, int bufferSize = 64 * 1024)
    {
        if (closing)
        {
            throw new ObjectDisposedException(nameof(TcpAsyncClient));
        }

        if (Initialized)
        {
            throw new InvalidOperationException("Client already connected!");
        }

#if NET20 || NET35
        Exception? e = null;
        foreach (var addr in DnsClient.Default.GetHostAddresses(hostname))
        {
            var socket = CreateSocket(addr.AddressFamily);
            try
            {
                socket.BeginConnect(hostname, port, ConnectAsyncCallback, socket);
                return;
            }
            catch (Exception ex)
            {
                e = ex;
            }
        }
        if (e != null)
        {
            throw e;
        }

        throw new Exception("No target address found at dns!");
#else
        ConnectAsync(new DnsEndPoint(hostname, port), bufferSize);
#endif
    }

    /// <summary>Performs an asynchonous connect to the specified address and port.</summary>
    /// <remarks>This function returns immediately. Results are delivered by the <see cref="Error"/> / <see cref="Connected"/> events.</remarks>
    /// <param name="address">ip address to connect to.</param>
    /// <param name="port">port to connect to.</param>
    /// <param name="bufferSize">tcp buffer size in bytes.</param>
    public void ConnectAsync(IPAddress address, int port, int bufferSize = 64 * 1024)
    {
        if (closing)
        {
            throw new ObjectDisposedException(nameof(TcpAsyncClient));
        }

        if (Initialized)
        {
            throw new InvalidOperationException("Client already connected!");
        }

        RemoteEndPoint = new(address, port);
        BufferSize = bufferSize;
        ConnectAsync(RemoteEndPoint, bufferSize);
    }

    /// <summary>Performs an asynchronous connect to the specified address and port.</summary>
    /// <remarks>This function returns immediately. Results are delivered by the <see cref="Error"/> / <see cref="Connected"/> events.</remarks>
    /// <param name="endPoint">ip endpoint to connect to.</param>
    /// <param name="bufferSize">tcp buffer size in bytes.</param>
    public void ConnectAsync(EndPoint endPoint, int bufferSize = 64 * 1024)
    {
        EnterLock();
        try
        {
            var socket = CreateSocket(endPoint.AddressFamily);
            socket.ReceiveBufferSize = bufferSize;
            socket.SendBufferSize = bufferSize;
            var e = new SocketAsyncEventArgs
            {
                RemoteEndPoint = endPoint,
                UserToken = socket,
            };
            e.Completed += ConnectAsyncCallback;
            var isPending = socket.ConnectAsync(e);
            void PendingCallback() => ConnectAsyncCallback(socket, e);
            if (!isPending) Task.Factory.StartNew(PendingCallback);
        }
        finally
        {
            ExitLock();
        }
    }

    /// <summary>Connects to the specified host and port.</summary>
    /// <param name="proxy">Proxy settings</param>
    /// <param name="host">host to connect to..</param>
    /// <param name="port">port to connect to.</param>
    public void ConnectViaProxy(Proxy proxy, string host, int port)
    {
        var request = WebRequest.Create(proxy.GetUri());
        var webProxy = new WebProxy(proxy.GetUri());
        request.Proxy = webProxy;
        request.Method = "CONNECT";
        if (proxy.Credentials is null)
        {
            webProxy.UseDefaultCredentials = true;
        }
        else
        {
            webProxy.Credentials = proxy.Credentials;
        }
        var response = request.GetResponse();
        try
        {
            const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
            var responseStream = response.GetResponseStream() ?? throw new InvalidOperationException("Could not get ResponseStream!");
            var connection = responseStream.GetPropertyValue("Connection", PrivateInstance) ?? throw new InvalidOperationException("Could not get Connection!");
            var networkStream = connection.GetPropertyValue("NetworkStream", PrivateInstance) ?? throw new InvalidOperationException("Could not get NetworkStream!");
            var socket = (networkStream.GetPropertyValue("Socket") as Socket) ?? throw new InvalidCastException("Could not cast Socket!");
            InitializeSocket(socket);
            StartReader();
        }
        catch (Exception ex)
        {
            OnError(ex);
        }
    }

    /// <summary>Releases unmanaged and managed resources.</summary>
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    /// <summary>Gets the stream.</summary>
    /// <returns>Returns the <see cref="Stream"/> instance used to send and receive data.</returns>
    /// <remarks>This function and access to all stream functions are threadsafe.</remarks>
    /// <exception cref="InvalidOperationException">Not connected.</exception>
    public virtual Stream GetStream() => Stream;

    /// <summary>Sends data to a connected remote.</summary>
    /// <remarks>This function is threadsafe.</remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    public void Send(byte[] buffer) => Send(buffer, 0, buffer.Length);

    /// <summary>Sends data to a connected remote.</summary>
    /// <remarks>This function is threadsafe.</remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="length">The number of bytes.</param>
    public void Send(byte[] buffer, int length) => Send(buffer, 0, length);

    /// <summary>Sends data to a connected remote.</summary>
    /// <remarks>This function is threadsafe.</remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="offset">The start offset at the byte array.</param>
    /// <param name="length">The number of bytes.</param>
    public void Send(byte[] buffer, int offset, int length)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("Not connected!");
        }
        EnterLock();
        try
        {
            CheckedSocket.Send(buffer, offset, length, 0);
            OnSent(buffer, offset, length);
        }
        catch (Exception ex)
        {
            OnError(ex);
            Close();
            throw;
        }
        finally
        {
            ExitLock();
        }
        Interlocked.Add(ref bytesSent, length - offset);
    }

    /// <summary>Sends data asynchronously to a connected remote.</summary>
    /// <remarks>
    /// This function is threadsafe, however calling this method more than one time prior completion may result in a different byte sequence at the receiver.
    /// </remarks>
    /// <remarks>This function is threadsafe.</remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    public void SendAsync(byte[] buffer, Action? callback = null) => InternalSendAsync(buffer, 0, buffer.Length, callback);

    /// <summary>Sends data asynchronously to a connected remote.</summary>
    /// <remarks>
    /// This function is threadsafe, however calling this method more than one time prior completion may result in a different byte sequence at the receiver.
    /// </remarks>
    /// <remarks>This function is threadsafe.</remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="length">The number of bytes.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    public void SendAsync(byte[] buffer, int length, Action? callback = null) => InternalSendAsync(buffer, 0, length, callback);

    /// <summary>Sends data asynchronously to a connected remote.</summary>
    /// <remarks>
    /// This function is threadsafe, however calling this method more than one time prior completion may result in a different byte sequence at the receiver.
    /// </remarks>
    /// <remarks>This function is threadsafe.</remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="offset">The start offset at the byte array.</param>
    /// <param name="length">The number of bytes.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    public void SendAsync(byte[] buffer, int offset, int length, Action? callback = null) => InternalSendAsync(buffer, offset, length, callback);

    /// <summary>Sends data asynchronously to a connected remote.</summary>
    /// <remarks>
    /// This function is threadsafe, however calling this method more than one time prior completion may result in a different byte sequence at the receiver.
    /// </remarks>
    /// <remarks>This function is threadsafe.</remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
    public void SendAsync<T>(byte[] buffer, Action<T> callback, T state) => InternalSendAsync(buffer, 0, buffer.Length, callback, state);

    /// <summary>Sends data asynchronously to a connected remote.</summary>
    /// <remarks>
    /// This function is threadsafe, however calling this method more than one time prior completion may result in a different byte sequence at the receiver.
    /// </remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="length">The number of bytes.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
    public void SendAsync<T>(byte[] buffer, int length, Action<T> callback, T state) => InternalSendAsync(buffer, 0, length, callback, state);

    /// <summary>Sends data asynchronously to a connected remote.</summary>
    /// <remarks>
    /// This function is threadsafe, however calling this method more than one time prior completion may result in a different byte sequence at the receiver.
    /// </remarks>
    /// <param name="buffer">An array of bytes to be send.</param>
    /// <param name="offset">The start offset at the byte array.</param>
    /// <param name="length">The number of bytes.</param>
    /// <param name="callback">Callback method to be called after completion.</param>
    /// <param name="state">State to pass to the callback.</param>
    /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
    public void SendAsync<T>(byte[] buffer, int offset, int length, Action<T> callback, T state) => InternalSendAsync(buffer, offset, length, callback, state);

    /// <inheritdoc/>
    public override string ToString()
    {
        if (RemoteEndPoint.AddressFamily == AddressFamily.InterNetworkV6)
        {
#if !NET20 && !NET35 && !NET40
            if (RemoteEndPoint.Address.IsIPv4MappedToIPv6)
            {
                return $"tcp://{RemoteEndPoint.Address.MapToIPv4()}:{RemoteEndPoint.Port}";
            }
#endif
            return $"tcp://[{RemoteEndPoint.Address}]:{RemoteEndPoint.Port}";
        }
        return $"tcp://{RemoteEndPoint}";
    }

    record ConnectResult(IPAddress Address, TcpAsyncClient Client) : BaseRecord;

    /// <summary>
    /// Tries to connect to any of the specified <paramref name="addresses"/>. 
    /// The first successfully connected client will be returned.
    /// All other already started or opening connections will be disposed.
    /// </summary>
    /// <param name="addresses">Address list to use for connection tries.</param>
    /// <param name="port">Port to connect to</param>
    /// <param name="timeout">Timeout in msec</param>
    /// <returns>Returns a connected <see cref="TcpAsyncClient"/> instance.</returns>
    /// <exception cref="TimeoutException"></exception>
    public static TcpAsyncClient TryConnect(IEnumerable<IPAddress> addresses, ushort port, int timeout = default)
    {
        var ready = new ManualResetEvent(false);
        void PrivateConnected(object? sender, EventArgs e) => ready.Set();
        ConnectResult PrivateConnect(IPAddress address) => new(address, TcpAsyncClient.TryConnectAsync(address, port, PrivateConnected, timeout));
        var list = addresses.Select(PrivateConnect).ToList();
        try
        {
            if (timeout > 0)
            {
                if (!ready.WaitOne(timeout)) throw new TimeoutException();
            }
            else
            {
                if (!ready.WaitOne()) throw new TimeoutException();
            }
            var connected = list.First(i => i.Client.IsConnected);
            list.Remove(connected);
            return connected.Client;
        }
        finally
        {
            list.ForEach(i => i.Client.Dispose());
        }
    }

    /// <summary>
    /// Creates a new <see cref="TcpAsyncClient"/> instance and starts a new connection attempt to the specified endpoint.
    /// </summary>
    /// <param name="address">Address to connect to.</param>
    /// <param name="port">Port to connect to.</param>
    /// <param name="connected">Event to be called on successful connection. (See <see cref="TcpAsyncClient.Error"/> for errors)</param>
    /// <param name="timeout">Timeout for the connection.</param>
    /// <returns>Returns the new <see cref="TcpAsyncClient"/> instance trying to connect.</returns>
    public static TcpAsyncClient TryConnectAsync(IPAddress address, ushort port, EventHandler<EventArgs>? connected, int timeout = 0)
    {
        var client = new TcpAsyncClient();
        if (connected is not null) client.Connected += connected;
        client.ConnectTimeout = timeout;
        client.ConnectAsync(new IPEndPoint(address, port));
        return client;
    }

    #endregion Public Methods
}
