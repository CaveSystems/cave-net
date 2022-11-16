using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;

namespace Cave.Net
{
    /// <summary>
    /// Provides a fast TcpServer implementation.
    /// </summary>
    /// <typeparam name="TClient">The type of the client.</typeparam>
    /// <seealso cref="IDisposable" />
    [ComVisible(false)]
    public class TcpServer<TClient> : ITcpServer
        where TClient : TcpAsyncClient, new()
    {
#if NETSTANDARD13 || NET20
        readonly Dictionary<SocketAsyncEventArgs, SocketAsyncEventArgs> pendingAccepts = new();
        readonly Dictionary<TClient, TClient> clients = new();

        void AddPendingAccept(SocketAsyncEventArgs e) => pendingAccepts.Add(e, e);

        void AddClient(TClient client) => clients.Add(client, client);

        void RemoveClient(TClient client)
        {
            if (clients.ContainsKey(client))
            {
                clients.Remove(client);
            }
        }

        IEnumerable<TClient> ClientList => clients.Keys;
        IEnumerable<SocketAsyncEventArgs> PendingAcceptList => pendingAccepts.Keys;
#else
        IEnumerable<TClient> ClientList => clients;
        readonly HashSet<SocketAsyncEventArgs> pendingAccepts = new();
        readonly HashSet<TClient> clients = new();

        void AddPendingAccept(SocketAsyncEventArgs e) => pendingAccepts.Add(e);

        void AddClient(TClient client) => clients.Add(client);

        void RemoveClient(TClient client)
        {
            if (clients.Contains(client))
            {
                clients.Remove(client);
            }
        }

        IEnumerable<SocketAsyncEventArgs> PendingAcceptList => pendingAccepts;
#endif

        Socket socket;
        int acceptBacklog = 20;
        int acceptThreads = Environment.ProcessorCount * 2;
        int tcpBufferSize = 64 * 1024;
        bool shutdown;
        int acceptWaiting;
        bool noExclusiveAddressUse;

        void AcceptStart()
        {
            while (true)
            {
                SocketAsyncEventArgs asyncAccept;
                lock (pendingAccepts)
                {
                    if (pendingAccepts.Count >= AcceptThreads)
                    {
                        return;
                    }

                    asyncAccept = new SocketAsyncEventArgs();
                    AddPendingAccept(asyncAccept);
                }

                // accept async or sync, call AcceptCompleted in any case
                Interlocked.Increment(ref acceptWaiting);
                asyncAccept.Completed += AcceptCompleted;

                bool pending;
                try
                {
                    pending = socket?.AcceptAsync(asyncAccept) == true;
                }
                catch (ObjectDisposedException)
                {
                    // shutdown
                    Trace.WriteLine("TcpServer socket shutdown.");
                    return;
                }

                if (!pending)
                {
                    void AcceptCompletedAction(object o) => AcceptCompleted(this, (SocketAsyncEventArgs)o);
#if NET20 || NET35
                    ThreadPool.QueueUserWorkItem(AcceptCompletedAction, asyncAccept);
#else
                    System.Threading.Tasks.Task.Factory.StartNew(AcceptCompletedAction, asyncAccept);
#endif
                }
            }
        }

        void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
        AcceptCompletedBegin:
            var waiting = Interlocked.Decrement(ref acceptWaiting);
            if (waiting == 0)
            {
                OnAcceptTasksBusy();
            }

            // handle accepted socket
            {
                var acceptedSocket = e.AcceptSocket;
                if (acceptedSocket?.Connected == true)
                {
                    // create client
                    var client = new TClient();
                    client.Disconnected += ClientDisconnected;
                    try
                    {
                        // add to my client list
                        lock (clients)
                        {
                            AddClient(client);
                        }

                        // initialize client instance with server and socket
                        client.InitializeServer(this, acceptedSocket);

                        // call client accepted event
                        OnClientAccepted(client);

                        // start processing of incoming data
                        client.StartReader();
                    }
                    catch (Exception ex)
                    {
                        OnClientException(client, ex);
                        client.Close();
                    }
                }
            }

            // start next socket accept
            if (!shutdown && !disposed)
            {
                // accept next
                Interlocked.Increment(ref acceptWaiting);
                e.AcceptSocket = null;
                bool pending;
                try
                {
                    pending = socket?.AcceptAsync(e) == true;
                }
                catch (ObjectDisposedException)
                {
                    // shutdown
                    Trace.WriteLine("TcpServer socket shutdown.");
                    return;
                }
                if (!pending)
                {
                    // AcceptCompleted(this, e);
                    goto AcceptCompletedBegin;

                    // we could do a function call to myself here but with slow OnClientAccepted() functions and fast networks we might get a stack overflow caused by infinite recursion
                }
            }
        }

        void ClientDisconnected(object sender, EventArgs e)
        {
            // perform cleanup of client list
            var client = (TClient)sender;
            lock (clients)
            {
                RemoveClient(client);
            }
        }

        /// <summary>
        /// Calls the <see cref="ClientException"/> event (if set).
        /// </summary>
        /// <param name="source">The source of the exception.</param>
        /// <param name="exception">The exception.</param>
        protected virtual void OnClientException(TClient source, Exception exception) => ClientException?.Invoke(this, new TcpServerClientExceptionEventArgs<TClient>(source, exception));

        /// <summary>
        /// Calls the <see cref="AcceptTasksBusy"/> event (if set).
        /// </summary>
        protected virtual void OnAcceptTasksBusy() => AcceptTasksBusy?.Invoke(this, new EventArgs());

        /// <summary>
        /// Calls the <see cref="ClientAccepted"/> event (if set).
        /// </summary>
        /// <param name="client">The client that was accepted.</param>
        protected virtual void OnClientAccepted(TClient client) => ClientAccepted?.Invoke(this, new TcpServerClientEventArgs<TClient>(client));

        /// <summary>Initializes a new instance of the <see cref="TcpServer{TClient}"/> class.</summary>
        public TcpServer()
        {
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Socket allows only one process to bind to a port or not.
        /// </summary>
        /// <remarks>
        /// Set to true if the Socket allows only one socket to bind to a specific port; otherwise, false.
        /// The default is true.
        /// </remarks>
        public bool ExclusiveAddressUse
        {
            get => !noExclusiveAddressUse;
            set
            {
                if (socket != null)
                {
                    throw new InvalidOperationException("Socket is already bound!");
                }

                noExclusiveAddressUse = !value;
            }
        }

        /// <summary>Listens at the specified <paramref name="address"/> and <paramref name="port"/>.</summary>
        /// <param name="address">The ip address to listen at.</param>
        /// <param name="port">The port to listen at.</param>
        public void Listen(IPAddress address, int port) => Listen(new IPEndPoint(address, port));

        /// <summary>Listens at the specified end point.</summary>
        /// <param name="endPoint">The end point.</param>
        /// <exception cref="ObjectDisposedException">TcpServer.</exception>
        public void Listen(IPEndPoint endPoint)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(TcpServer));
            }
            if (socket != null)
            {
                throw new InvalidOperationException("Socket is already listening!");
            }

            socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            switch (endPoint.AddressFamily)
            {
                case AddressFamily.InterNetwork:
                    break;
                case AddressFamily.InterNetworkV6:
                    socket.EnableDualSocket();
                    break;
            }

            socket.ExclusiveAddressUse = !noExclusiveAddressUse;
            socket.Bind(endPoint);
            socket.Listen(AcceptBacklog);
            LocalEndPoint = (IPEndPoint)socket.LocalEndPoint;
            AcceptStart();
        }

        /// <summary>Listens at the specified port on IPv4 and IPv6 if available.</summary>
        /// <param name="port">The port.</param>
        /// <exception cref="ObjectDisposedException">TcpServer.</exception>
        public void Listen(int port) => Listen(port, null);

        /// <summary>Listens at the specified port.</summary>
        /// <param name="port">The port.</param>
        /// <param name="useIPv6">Use dualstack socket. Defaults value is true.</param>
        /// <exception cref="ObjectDisposedException">TcpServer.</exception>
        public void Listen(int port, bool? useIPv6 = null)
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(TcpServer));
            }
#if NETSTANDARD13
            Listen(new IPEndPoint(IPAddress.Any, port));
#else
            useIPv6 ??= NetworkInterface.GetAllNetworkInterfaces().Any(n => n.GetIPProperties().UnicastAddresses.Any(u => u.Address.AddressFamily == AddressFamily.InterNetworkV6));
            if (useIPv6.GetValueOrDefault(true))
            {
                Listen(new IPEndPoint(IPAddress.IPv6Any, port));
            }
            else
            {
                Listen(new IPEndPoint(IPAddress.Any, port));
            }
#endif
        }

        /// <summary>Disconnects all clients.</summary>
        public void DisconnectAllClients()
        {
            lock (clients)
            {
                foreach (var c in Clients)
                {
                    c.Close();
                }
                clients.Clear();
            }
        }

        /// <summary>Closes this instance.</summary>
        public void Close()
        {
            shutdown = true;
            lock (pendingAccepts)
            {
                foreach (var e in PendingAcceptList)
                {
                    e.Dispose();
                }
                pendingAccepts.Clear();

                if (socket != null)
                {
#if NETSTANDARD13
                    socket.Dispose();
#else
                    socket.Close();
#endif
                    socket = null;
                }
            }

            DisconnectAllClients();
            Dispose();
        }

        /// <summary>Gets or sets the maximum number of pending connections.</summary>
        /// <value>The maximum length of the pending connections queue.</value>
        /// <remarks>On high load this should be 10 x <see cref="AcceptThreads"/>.</remarks>
        /// <exception cref="InvalidOperationException">Socket is already listening.</exception>
        public int AcceptBacklog
        {
            get => acceptBacklog;
            set
            {
                if (socket != null)
                {
                    throw new InvalidOperationException("Socket is already listening!");
                }

                acceptBacklog = Math.Max(1, value);
            }
        }

        /// <summary>Gets or sets the number of threads used to accept connections.</summary>
        /// <value>The maximum length of the pending connections queue.</value>
        /// <exception cref="InvalidOperationException">Socket is already listening.</exception>
        public int AcceptThreads
        {
            get => acceptThreads;
            set
            {
                if (socket != null)
                {
                    throw new InvalidOperationException("Socket is already listening!");
                }

                acceptThreads = Math.Max(1, value);
            }
        }

        /// <summary>Gets or sets the size of the buffer used when receiving data.</summary>
        /// <value>The size of the buffer.</value>
        /// <exception cref="InvalidOperationException">Socket is already listening.</exception>
        /// <exception cref="ArgumentOutOfRangeException">value.</exception>
        public int BufferSize
        {
            get => tcpBufferSize;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (socket != null)
                {
                    throw new InvalidOperationException("Socket is already listening!");
                }

                tcpBufferSize = value;
            }
        }

        /// <summary>Gets or sets the amount of time, in milliseconds, thata read operation blocks waiting for data.</summary>
        /// <value>A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value, <see cref="Timeout.Infinite"/>, specifies that the read operation does not time out.</value>
        public int ReceiveTimeout { get; set; } = Timeout.Infinite;

        /// <summary>Gets or sets the amount of time, in milliseconds, thata write operation blocks waiting for data.</summary>
        /// <value>A Int32 that specifies the amount of time, in milliseconds, that will elapse before a write operation fails. The default value, <see cref="Timeout.Infinite"/>, specifies that the write operation does not time out.</value>
        public int SendTimeout { get; set; } = Timeout.Infinite;

        /// <summary>Gets the local end point.</summary>
        /// <value>The local end point.</value>
        public IPEndPoint LocalEndPoint { get; private set; }

        /// <summary>Gets a value indicating whether this instance is listening.</summary>
        /// <value>
        /// <c>true</c> if this instance is listening; otherwise, <c>false</c>.
        /// </value>
        public bool IsListening => socket != null && socket.IsBound;

        /// <summary>
        /// Event to be called whenever all accept tasks get busy. This may indicate declined connections attempts (due to a full backlog).
        /// </summary>
        public event EventHandler<EventArgs> AcceptTasksBusy;

        /// <summary>
        /// Event to be called after a client was accepted occured
        /// </summary>
        public event EventHandler<TcpServerClientEventArgs<TClient>> ClientAccepted;

        /// <summary>
        /// Event to be called after a client exception occured that cannot be handled by the clients Error event.
        /// </summary>
        public event EventHandler<TcpServerClientExceptionEventArgs<TClient>> ClientException;

        /// <summary>Gets all connected clients.</summary>
        /// <value>The clients.</value>
        public TClient[] Clients
        {
            get
            {
                lock (clients)
                {
                    return ClientList.ToArray();
                }
            }
        }

        #region IDisposable Support
        bool disposed;

        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (socket != null)
                {
                    (socket as IDisposable)?.Dispose();
                    socket = null;
                }
                disposed = true;
            }
        }

        /// <summary>Releases unmanaged and managed resources.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        /// <inheritdoc/>
        public override string ToString() =>
            LocalEndPoint.Address.AddressFamily == AddressFamily.InterNetwork
            ? $"tcp://{LocalEndPoint}"
            : $"tcp://[{LocalEndPoint.Address}]:{LocalEndPoint.Port}";
    }
}
