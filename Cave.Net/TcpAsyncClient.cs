using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cave.IO;
#if NETSTANDARD13 || NETSTANDARD20 || NET45 || NET46 || NET47
using System.Threading.Tasks;
#endif

namespace Cave.Net
{
    /// <summary>
    /// Provides an async tcp client implementation.
    /// </summary>
    [DebuggerDisplay("{RemoteEndPoint}")]
    public class TcpAsyncClient : IDisposable
    {
        class AsyncParameters
        {
            public AsyncParameters(Socket socket, int bufferSize)
            {
                Socket = socket;
                BufferSize = bufferSize;
            }

            public Socket Socket { get; }

            public int BufferSize { get; }
        }

        #region private class
        bool connectedEventTriggered;
        bool closing;
        bool initialized;
        bool disconnectedEventTriggered;
        long bytesReceived;
        long bytesSent;
        int pendingAsyncSends;
        SocketAsyncEventArgs socketAsync;
        int receiveTimeout;
        int sendTimeout;
        short ttl;
        bool nodelay;
        LingerOption lingerState;
        Socket uncheckedSocket;

        Socket CheckedSocket
        {
            get
            {
                if (uncheckedSocket == null)
                {
                    throw new InvalidOperationException("Not connected!");
                }
                if (closing)
                {
                    throw new ObjectDisposedException(nameof(TcpAsyncClient));
                }
                return uncheckedSocket;
            }
        }

        T CachedValue<T>(ref T field, Func<T> func)
        {
            if (uncheckedSocket != null && !closing)
            {
                field = func();
            }
            return field;
        }

        Socket CreateSocket()
        {
            if (closing)
            {
                throw new ObjectDisposedException(nameof(TcpAsyncClient));
            }
#if NETSTANDARD13 || NET20 || NET35 || NET40
            uncheckedSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
#else
            uncheckedSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
#endif
            uncheckedSocket.ExclusiveAddressUse = false;
            uncheckedSocket.LingerState = new LingerOption(false, 0);
            uncheckedSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            return uncheckedSocket;
        }

        void InitializeSocket(Socket socket)
        {
            if (initialized)
            {
                throw new InvalidOperationException("Already initialized!");
            }
            this.uncheckedSocket = socket ?? throw new ArgumentNullException(nameof(socket));
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
            LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
            initialized = true;
        }

        /// <summary>Gets called whenever a read is completed.</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SocketAsyncEventArgs"/> instance containing the event data.</param>
        void ReadCompleted(object sender, SocketAsyncEventArgs e)
        {
ReadCompletedBegin:
            if (closing)
            {
                return;
            }

            var bytesTransferred = e.BytesTransferred;

            switch (e.SocketError)
            {
                case SocketError.Success:
                    break;
                case SocketError.ConnectionReset:
                    Close();
                    return;
                default:
                    OnError(new SocketException((int)e.SocketError));
                    Close();
                    return;
            }

            try
            {
                // got data (if not this is the disconnected call)
                if (bytesTransferred > 0)
                {
                    Interlocked.Add(ref bytesReceived, bytesTransferred);

                    // call event
                    OnReceived(e.Buffer, e.Offset, bytesTransferred, out bool handled);
                    if (!handled)
                    {
                        // cleanup read buffers and add new data
                        lock (ReceiveBuffer)
                        {
                            ReceiveBuffer?.FreeBuffers();
                            ReceiveBuffer?.AppendBuffer(e.Buffer, e.Offset, bytesTransferred);
                            OnBuffered();
                            Monitor.PulseAll(ReceiveBuffer);
                        }
                    }

                    // still connected after event ?
                    if (IsConnected)
                    {
                        // yes read again
                        var isPending = CheckedSocket.ReceiveAsync(socketAsync);
                        if (!isPending)
                        {
                            e = socketAsync;
                            goto ReadCompletedBegin;

                            // we could do a function call to myself here but with slow OnReceived() functions and fast networks we might get a stack overflow caused by infinite recursion
                            // spawning threads using the threadpool is not a good idea either, because multiple receives will mess up our (sequential) stream reading.
                        }
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
            Close();
        }

        void EnterLock()
        {
            if (!Monitor.TryEnter(this, DeadLockTimeout))
            {
                throw new TimeoutException($"DeadLock timeout exceeded. This can be caused by inproper use of {nameof(TcpAsyncClient)} events!");
            }
        }

        void ExitLock() => Monitor.Exit(this);

        #endregion

        #region internal functions used by TcpSocketServer

        /// <summary>
        /// Initializes the client for use with the specified <paramref name="server"/> instance.
        /// </summary>
        /// <exception cref="InvalidOperationException">Reader already started.</exception>
        /// <param name="server">Server instance this client belongs to.</param>
        /// <param name="socket">Socket instance this client uses.</param>
        protected internal virtual void InitializeServer(ITcpServer server, Socket socket)
        {
            if (initialized)
            {
                throw new InvalidOperationException("Already initialized!");
            }
            Server = server ?? throw new ArgumentNullException(nameof(server));
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            socket.ReceiveTimeout = server.ReceiveTimeout;
            socket.SendTimeout = server.SendTimeout;
            socket.LingerState = new LingerOption(false, 0);
            InitializeSocket(socket);
        }

        /// <summary>
        /// Calls the <see cref="OnConnect()"/> function and starts the async socket reader.
        /// </summary>
        /// <param name="bufferSize">The <see cref="Socket.SendBufferSize"/> and <see cref="Socket.ReceiveBufferSize"/> to be used.</param>
        internal void StartReader(int bufferSize)
        {
            if (socketAsync != null)
            {
                throw new InvalidOperationException("Reader already started!");
            }
            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }
            BufferSize = bufferSize;
            OnConnect();
            CheckedSocket.SendBufferSize = bufferSize;
            CheckedSocket.ReceiveBufferSize = bufferSize;
            var buffer = new byte[bufferSize];
            socketAsync = new SocketAsyncEventArgs() { UserToken = this, };
            socketAsync.Completed += ReadCompleted;
            socketAsync.SetBuffer(buffer, 0, buffer.Length);
            var isPending = CheckedSocket.ReceiveAsync(socketAsync);
            if (!isPending)
            {
#if NET20 || NET35 || NET40
                ThreadPool.QueueUserWorkItem((e) =>
                {
                    ReadCompleted(this, (SocketAsyncEventArgs)e);
                }, socketAsync);
#else
                Task.Factory.StartNew((e) =>
                {
                    ReadCompleted(this, (SocketAsyncEventArgs)e);
                }, socketAsync);
#endif
            }
        }

        #endregion

        #region events

        /// <summary>
        /// Calls the <see cref="Connected"/> event (if set).
        /// </summary>
        protected virtual void OnConnect()
        {
            if (connectedEventTriggered)
            {
                throw new InvalidOperationException("OnConnect triggered twice!");
            }

            connectedEventTriggered = true;
            Connected?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Calls the <see cref="Disconnected"/> event (if set).
        /// </summary>
        protected virtual void OnDisconnect()
        {
            if (connectedEventTriggered && !disconnectedEventTriggered)
            {
                disconnectedEventTriggered = true;
                Disconnected?.Invoke(this, new EventArgs());
            }
        }

        /// <summary>
        /// Calls the <see cref="Received"/> event (if set).
        /// </summary>
        /// <remarks>
        /// You can set <see cref="BufferEventArgs.Handled"/> to true when overrideing this function or within <see cref="Received"/>
        /// to skip adding data to the <see cref="Stream"/> and <see cref="ReceiveBuffer"/>.
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

        /// <summary>
        /// Calls the <see cref="Buffered"/> event (if set).
        /// </summary>
        protected virtual void OnBuffered()
        {
            Buffered?.Invoke(this, new EventArgs());
        }

        /// <summary>Calls the Error event (if set) and closes the connection.</summary>
        /// <param name="ex">The exception (most of the time this will be a <see cref="SocketException"/>.</param>
        protected internal virtual void OnError(Exception ex)
        {
            if (!closing)
            {
                Error?.Invoke(this, new ExceptionEventArgs(ex));
                Close();
            }
        }

        /// <summary>
        /// Event to be called after the connection was established
        /// </summary>
        public event EventHandler<EventArgs> Connected;

        /// <summary>
        /// Event to be called after the connection was closed
        /// </summary>
        public event EventHandler<EventArgs> Disconnected;

        /// <summary>
        /// Event to be called after a buffer was received
        /// </summary>
        public event EventHandler<BufferEventArgs> Received;

        /// <summary>
        /// Event to be called after a buffer was received and was not handled by the <see cref="Received"/> event
        /// </summary>
        public event EventHandler<EventArgs> Buffered;

        /// <summary>
        /// Event to be called after an error was encountered
        /// </summary>
        public event EventHandler<ExceptionEventArgs> Error;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpAsyncClient"/> class.
        /// </summary>
        public TcpAsyncClient() => Stream = new TcpAsyncStream(this);

        #region public functions

#if NETSTANDARD13
        void Connect(AsyncParameters parameters, Task task)
        {
            EnterLock();
            try
            {
                task.Wait(ConnectTimeout);
                if (task.IsFaulted)
                {
                    throw task.Exception;
                }

                if (task.IsCompleted)
                {
                    InitializeSocket(parameters.Socket);
                    StartReader(parameters.BufferSize);
                    return;
                }
                Close();
                throw new TimeoutException();
            }
            catch (Exception ex)
            {
                OnError(ex);
                throw;
            }
            finally
            {
                ExitLock();
            }
        }
#else
        void Connect(AsyncParameters parameters, IAsyncResult asyncResult)
        {
            EnterLock();
            try
            {
                if (asyncResult.AsyncWaitHandle.WaitOne(ConnectTimeout))
                {
                    parameters.Socket.EndConnect(asyncResult);
                    InitializeSocket(parameters.Socket);
                    StartReader(parameters.BufferSize);
                }
                else
                {
                    Close();
                    throw new TimeoutException();
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
                throw;
            }
            finally
            {
                ExitLock();
            }
        }
#endif

        void ConnectAsyncCallback(object sender, SocketAsyncEventArgs e)
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
                if (!CheckedSocket.Connected)
                {
                    OnError(new SocketException((int)SocketError.SocketError));
                    return;
                }
                try
                {
                    var parameters = (AsyncParameters)e.UserToken;
                    InitializeSocket(parameters.Socket);
                    StartReader(parameters.BufferSize);
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

#if NET20 || NET35
        void ConnectAsyncCallback(IAsyncResult asyncResult)
        {
            EnterLock();
            try
            {
                var parameters = (AsyncParameters)asyncResult.AsyncState;
                if (!parameters.Socket.Connected)
                {
                    OnError(new SocketException((int)SocketError.SocketError));
                    return;
                }
                parameters.Socket.EndConnect(asyncResult);
                InitializeSocket(parameters.Socket);
                StartReader(parameters.BufferSize);
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
#endif

        /// <summary>
        /// Connects to the specified hostname and port.
        /// </summary>
        /// <param name="hostname">hostname to resolve.</param>
        /// <param name="port">port to connect to.</param>
        /// <param name="bufferSize">tcp buffer size in bytes.</param>
        public void Connect(string hostname, int port, int bufferSize = 64 * 1024)
        {
            if (closing)
            {
                throw new ObjectDisposedException(nameof(TcpAsyncClient));
            }

            if (initialized)
            {
                throw new InvalidOperationException("Client already connected!");
            }
            var socket = CreateSocket();
            var parameters = new AsyncParameters(socket, bufferSize);
#if NETSTANDARD13
            Connect(parameters, socket.ConnectAsync(hostname, port));
#else
            Connect(parameters, socket.BeginConnect(hostname, port, null, null));
#endif
        }

        /// <summary>
        /// Connects to the specified hostname and port.
        /// </summary>
        /// <param name="hostname">hostname to resolve.</param>
        /// <param name="port">port to connect to.</param>
        /// <param name="bufferSize">tcp buffer size in bytes.</param>
        public void ConnectAsync(string hostname, int port, int bufferSize = 64 * 1024)
        {
            if (closing)
            {
                throw new ObjectDisposedException(nameof(TcpAsyncClient));
            }

            if (initialized)
            {
                throw new InvalidOperationException("Client already connected!");
            }

#if NET20 || NET35
            var socket = CreateSocket();
            socket.BeginConnect(hostname, port, ConnectAsyncCallback, new AsyncParameters(socket, bufferSize));
#else
            ConnectAsync(new DnsEndPoint(hostname, port), bufferSize);
#endif
        }

        /// <summary>
        /// Connects to the specified address and port.
        /// </summary>
        /// <param name="address">ip address to connect to.</param>
        /// <param name="port">port to connect to.</param>
        /// <param name="bufferSize">tcp buffer size in bytes.</param>
        public void Connect(IPAddress address, int port, int bufferSize = 64 * 1024)
        {
            if (closing)
            {
                throw new ObjectDisposedException(nameof(TcpAsyncClient));
            }

            if (initialized)
            {
                throw new InvalidOperationException("Client already connected!");
            }

            RemoteEndPoint = new IPEndPoint(address, port);
            var socket = CreateSocket();
            var parameters = new AsyncParameters(socket, bufferSize);
#if NETSTANDARD13
            Connect(parameters, socket.ConnectAsync(address, port));
#else
            Connect(parameters, socket.BeginConnect(address, port, null, null));
#endif
        }

        /// <summary>
        /// Performs an asynchonous connect to the specified address and port.
        /// </summary>
        /// <remarks>
        /// This function returns immediately.
        /// Results are delivered by the <see cref="Error"/> / <see cref="Connected"/> events.
        /// </remarks>
        /// <param name="address">ip address to connect to.</param>
        /// <param name="port">port to connect to.</param>
        /// <param name="bufferSize">tcp buffer size in bytes.</param>
        public void ConnectAsync(IPAddress address, int port, int bufferSize = 64 * 1024)
        {
            if (closing)
            {
                throw new ObjectDisposedException(nameof(TcpAsyncClient));
            }

            if (initialized)
            {
                throw new InvalidOperationException("Client already connected!");
            }

            RemoteEndPoint = new IPEndPoint(address, port);
            ConnectAsync(RemoteEndPoint, bufferSize);
        }

        /// <summary>
        /// Connects to the specified address and port.
        /// </summary>
        /// <param name="endPoint">ip endpoint to connect to.</param>
        /// <param name="bufferSize">tcp buffer size in bytes.</param>
        public void Connect(IPEndPoint endPoint, int bufferSize = 64 * 1024)
        {
            Connect(endPoint.Address, endPoint.Port, bufferSize);
        }

        /// <summary>
        /// Performs an asynchonous connect to the specified address and port.
        /// </summary>
        /// <remarks>
        /// This function returns immediately.
        /// Results are delivered by the <see cref="Error"/> / <see cref="Connected"/> events.
        /// </remarks>
        /// <param name="endPoint">ip endpoint to connect to.</param>
        /// <param name="bufferSize">tcp buffer size in bytes.</param>
        public void ConnectAsync(EndPoint endPoint, int bufferSize = 64 * 1024)
        {
            EnterLock();
            try
            {
                RemoteEndPoint = endPoint as IPEndPoint;
                var socket = CreateSocket();
                var e = new SocketAsyncEventArgs()
                {
                    RemoteEndPoint = endPoint,
                    UserToken = new AsyncParameters(socket, bufferSize),
                };
                e.Completed += ConnectAsyncCallback;
                var isPending = socket.ConnectAsync(e);
                if (!isPending)
                {
                    ConnectAsyncCallback(socket, e);
                }
            }
            finally
            {
                ExitLock();
            }
        }

        /// <summary>Gets the stream.</summary>
        /// <returns>Returns the <see cref="Stream"/> instance used to send and receive data.</returns>
        /// <remarks>This function and access to all stream functions are threadsafe.</remarks>
        /// <exception cref="InvalidOperationException">Not connected.</exception>
        public virtual Stream GetStream() => Stream;

        /// <summary>
        /// Sends data asynchronously to a connected remote.
        /// </summary>
        /// <remarks>
        /// This function is threadsafe, howeverc alling this method more than one time prior completion may result in a
        /// different byte sequence at the receiver.
        /// </remarks>
        /// <remarks>This function is threadsafe.</remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        /// <param name="callback">Callback method to be called after completion.</param>
        public void SendAsync(byte[] buffer, Action callback = null) => SendAsync(buffer, 0, buffer.Length, callback == null ? (Action<object>)null : (o) => callback());

        /// <summary>
        /// Sends data asynchronously to a connected remote.
        /// </summary>
        /// <remarks>
        /// This function is threadsafe, howeverc alling this method more than one time prior completion may result in a
        /// different byte sequence at the receiver.
        /// </remarks>
        /// <remarks>This function is threadsafe.</remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        /// <param name="length">The number of bytes.</param>
        /// <param name="callback">Callback method to be called after completion.</param>
        public void SendAsync(byte[] buffer, int length, Action callback = null) => SendAsync(buffer, 0, length, callback == null ? (Action<object>)null : (o) => callback());

        /// <summary>
        /// Sends data asynchronously to a connected remote.
        /// </summary>
        /// <remarks>
        /// This function is threadsafe, howeverc alling this method more than one time prior completion may result in a
        /// different byte sequence at the receiver.
        /// </remarks>
        /// <remarks>This function is threadsafe.</remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        /// <param name="offset">The start offset at the byte array.</param>
        /// <param name="length">The number of bytes.</param>
        /// <param name="callback">Callback method to be called after completion.</param>
        public void SendAsync(byte[] buffer, int offset, int length, Action callback = null) => SendAsync(buffer, offset, length, callback == null ? (Action<object>)null : (o) => callback());

        /// <summary>
        /// Sends data asynchronously to a connected remote.
        /// </summary>
        /// <remarks>
        /// This function is threadsafe, howeverc alling this method more than one time prior completion may result in a
        /// different byte sequence at the receiver.
        /// </remarks>
        /// <remarks>This function is threadsafe.</remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        /// <param name="callback">Callback method to be called after completion.</param>
        /// <param name="state">State to pass to the callback.</param>
        /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
        public void SendAsync<T>(byte[] buffer, Action<T> callback = null, T state = default(T)) => SendAsync(buffer, 0, buffer.Length, callback, state);

        /// <summary>
        /// Sends data asynchronously to a connected remote.
        /// </summary>
        /// <remarks>
        /// This function is threadsafe, howeverc alling this method more than one time prior completion may result in a
        /// different byte sequence at the receiver.
        /// </remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        /// <param name="length">The number of bytes.</param>
        /// <param name="callback">Callback method to be called after completion.</param>
        /// <param name="state">State to pass to the callback.</param>
        /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
        public void SendAsync<T>(byte[] buffer, int length, Action<T> callback = null, T state = default(T)) => SendAsync(buffer, 0, length, callback, state);

        /// <summary>
        /// Sends data asynchronously to a connected remote.
        /// </summary>
        /// <remarks>
        /// This function is threadsafe, howeverc alling this method more than one time prior completion may result in a
        /// different byte sequence at the receiver.
        /// </remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        /// <param name="offset">The start offset at the byte array.</param>
        /// <param name="length">The number of bytes.</param>
        /// <param name="callback">Callback method to be called after completion.</param>
        /// <param name="state">State to pass to the callback.</param>
        /// <typeparam name="T">Type for the callback <paramref name="state"/> parameter.</typeparam>
        public void SendAsync<T>(byte[] buffer, int offset, int length, Action<T> callback = null, T state = default(T))
        {
            void Completed(object s, SocketAsyncEventArgs e)
            {
                Interlocked.Decrement(ref pendingAsyncSends);
                Interlocked.Add(ref bytesSent, e.BytesTransferred);
                if (e.SocketError != SocketError.Success)
                {
                    OnError(new SocketException((int)e.SocketError));
                    Close();
                }
                e.Dispose();
                callback?.Invoke(state);
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
                callback?.Invoke(state);
                throw;
            }
            finally
            {
                ExitLock();
            }
        }

        /// <summary>
        /// Sends data to a connected remote.
        /// </summary>
        /// <remarks>This function is threadsafe.</remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        public void Send(byte[] buffer) => Send(buffer, 0, buffer.Length);

        /// <summary>
        /// Sends data to a connected remote.
        /// </summary>
        /// <remarks>This function is threadsafe.</remarks>
        /// <param name="buffer">An array of bytes to be send.</param>
        /// <param name="length">The number of bytes.</param>
        public void Send(byte[] buffer, int length) => Send(buffer, 0, length);

        /// <summary>
        /// Sends data to a connected remote.
        /// </summary>
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
            }
            catch (Exception ex)
            {
                OnError(ex);
                Close();
                throw;
            }
            finally
            {
                Monitor.Exit(this);
            }
            Interlocked.Add(ref bytesSent, length - offset);
        }

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
#if !NETSTANDARD13
                        uncheckedSocket.Close();
#endif
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
        #endregion

        #region IDisposable Support

        bool disposed;

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

        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            closing = true;
            DisposeUnmanaged();
        }

        /// <summary>Releases unmanaged and managed resources.</summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }
        #endregion

        #region public properties

        /// <summary>Gets the raw TCP stream used to send and receive data.</summary>
        /// <remarks>This function and access to all stream functions are threadsafe.</remarks>
        /// <value>The TCP stream instance.</value>
        public TcpAsyncStream Stream { get; }

        /// <summary>Gets the receive buffer.</summary>
        /// <value>The receive buffer.</value>
        /// <remarks>Use lock on this buffer to ensure thread safety when using concurrent access to the <see cref="Stream"/> property, <see cref="GetStream()"/> function and/or <see cref="Received"/> callbacks.</remarks>
        public FifoStream ReceiveBuffer { get; } = new FifoStream();

        /// <summary>Gets or sets the amount of time, in milliseconds, that a connect operation blocks waiting for data.</summary>
        /// <value>A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value, <see cref="Timeout.Infinite"/>, specifies that the connect operation does not time out.</value>
        public int ConnectTimeout { get; set; } = 5000;

        /// <summary>Gets a value indicating whether the client is connected.</summary>
        public bool IsConnected => !closing && (uncheckedSocket?.Connected ?? false);

        /// <summary>Gets the number of bytes received.</summary>
        public long BytesReceived => Interlocked.Read(ref bytesReceived);

        /// <summary>Gets the number of bytes sent.</summary>
        public long BytesSent => Interlocked.Read(ref bytesSent);

        /// <summary>
        /// Gets the number of active async send tasks.
        /// </summary>
        public int PendingAsyncSends => pendingAsyncSends;

        /// <summary>Gets the remote end point.</summary>
        /// <value>The remote end point.</value>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>Gets the local end point.</summary>
        /// <value>The local end point.</value>
        public IPEndPoint LocalEndPoint { get; private set; }

        /// <summary>Gets or sets the amount of time, in milliseconds, that a read operation blocks waiting for data.</summary>
        /// <value>A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value, <see cref="Timeout.Infinite"/>, specifies that the read operation does not time out.</value>
        /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
        public int ReceiveTimeout
        {
            get => CachedValue(ref receiveTimeout, () => uncheckedSocket.ReceiveTimeout);
            set => CheckedSocket.ReceiveTimeout = value;
        }

        /// <summary>Gets or sets the amount of time, in milliseconds, that a write operation blocks waiting for transmission.</summary>
        /// <value>A Int32 that specifies the amount of time, in milliseconds, that will elapse before a write operation fails. The default value, <see cref="Timeout.Infinite"/>, specifies that the write operation does not time out.</value>
        /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
        public int SendTimeout
        {
            get => CachedValue(ref sendTimeout, () => uncheckedSocket.SendTimeout);
            set => CheckedSocket.SendTimeout = value;
        }

        /// <summary>
        /// Gets or sets the dead lock timeout. This is the maximum time thread safe functions wait for acquiring the socket lock.
        /// </summary>
        public TimeSpan DeadLockTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets the buffer size used.
        /// </summary>
        public int BufferSize { get; private set; }

        /// <summary>
        /// Gets or sets a value that specifies the Time To Live (TTL) value of Internet Protocol (IP) packets sent by the Socket.
        /// </summary>
        /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
        public short Ttl
        {
            get => CachedValue(ref ttl, () => uncheckedSocket.Ttl);
            set => CheckedSocket.Ttl = value;
        }

        /// <summary>Gets or sets a value indicating whether the stream Socket is using the Nagle algorithm.</summary>
        /// <value><c>true</c> if the Socket uses the Nagle algorithm; otherwise, <c>false</c>.</value>
        /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
        public bool NoDelay
        {
            get => CachedValue(ref nodelay, () => uncheckedSocket.NoDelay);
            set => CheckedSocket.NoDelay = value;
        }

        /// <summary>
        /// Gets or sets a value that specifies whether the Socket will delay closing a socket in an attempt to send all pending data.
        /// </summary>
        /// <remarks>This cannot be accessed prior <see cref="Connect(string, int, int)"/>.</remarks>
        public LingerOption LingerState
        {
            get => CachedValue(ref lingerState, () => uncheckedSocket.LingerState);
            set => CheckedSocket.LingerState = value;
        }

        /// <summary>
        /// Gets the server instance this client belongs to. May be <c>null</c>.
        /// </summary>
        public ITcpServer Server { get; private set; }

        /// <summary>
        /// Gets or sets an user defined object.
        /// </summary>
        public object State { get; set; }

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>tcp://remoteip:port.</returns>
        public override string ToString()
        {
            return $"tcp://{RemoteEndPoint}";
        }
    }
}
