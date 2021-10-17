using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Cave.Net
{
    /// <summary>
    /// Provides a ssl server implementation accepting and authenticating <see cref="SslClient"/> connections.
    /// </summary>
    public class SslServer
    {
        #region private implementation
        readonly List<TcpListener> listeners = new();
        bool isClosed;

        void Listen(object obj)
        {
            var listener = obj as TcpListener;
            listener.Start();
            while (!isClosed)
            {
                // stop if listener closed
                lock (listeners)
                {
                    if (!listeners.Contains(listener))
                    {
                        break;
                    }
                }

                // accept client
                TcpClient tcpClient = null;
                try
                {
                    tcpClient = listener.AcceptTcpClient();
                    var sslClient = new SslClient(tcpClient);
                    sslClient.Authenticate += new EventHandler<SslAuthenticationEventArgs>(ClientAuthenticate);
                    sslClient.DoServerTLS(Certificate);
                    OnConnected(sslClient);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    try
                    {
                        if ((tcpClient != null) && tcpClient.Connected)
                        {
                            tcpClient.Close();
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        void ClientAuthenticate(object sender, SslAuthenticationEventArgs e) => OnAuthenticate(e);

        #endregion

        #region event starters

        /// <summary>
        /// Handles the incoming connection and calls the <see cref="Connected"/> event.
        /// </summary>
        /// <param name="client">The client.</param>
        protected virtual void OnConnected(SslClient client)
        {
            var evt = Connected;

            // return if event not used
            try
            {
                evt?.Invoke(this, new SslClientEventArgs(client));
            }
            catch (Exception ex)
            {
                if (client.Connected)
                {
                    try
                    {
                        client.Close();
                    }
                    catch
                    {
                    }
                }
                Trace.WriteLine(ex);
            }
        }

        /// <summary>
        /// Calls the <see cref="Authenticate"/> event.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        protected virtual void OnAuthenticate(SslAuthenticationEventArgs eventArgs) => Authenticate?.Invoke(this, eventArgs);

        #endregion

        #region public events

        /// <summary>
        /// Event to be executed on each new incoming connection
        /// </summary>
        public event EventHandler<SslClientEventArgs> Connected;

        /// <summary>
        /// Event to be executed on each new incoming connection to be authenticated. The event may prohibit authentication
        /// based on the certificate, chain and errors encountered
        /// </summary>
        public event EventHandler<SslAuthenticationEventArgs> Authenticate;
        #endregion

        #region public functionality

        /// <summary>
        /// Gets the certificate the server uses.
        /// </summary>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslServer"/> class.
        /// </summary>
        /// <param name="certificate">The certificate.</param>
        public SslServer(X509Certificate2 certificate) => Certificate = certificate ?? throw new ArgumentNullException(nameof(certificate));

        /// <summary>
        /// Starts listening at the specified port (IPv4).
        /// </summary>
        /// <param name="address">The ip address to listen at.</param>
        /// <param name="port">The port (1..65534) to listen at.</param>
        public void Listen(IPAddress address, int port)
        {
            if (isClosed)
            {
                throw new ObjectDisposedException("SslServer");
            }

            var listener = new TcpListener(address, port)
            {
                ExclusiveAddressUse = true,
            };
            Task.Factory.StartNew(new Action<object>(Listen), listener);
            listeners.Add(listener);
        }

        /// <summary>
        /// Starts listening at the specified local <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="iPEndPoint">The local IPEndPoint to listen at.</param>
        public void Listen(IPEndPoint iPEndPoint)
        {
            if (isClosed)
            {
                throw new ObjectDisposedException("SslServer");
            }

            var listener = new TcpListener(iPEndPoint)
            {
                ExclusiveAddressUse = true,
            };
            Task.Factory.StartNew(new Action<object>(Listen), listener);
            listeners.Add(listener);
        }

        /// <summary>
        /// Gets a list of local IPEndPoints currently listened at.
        /// </summary>
        public IPEndPoint[] LocalEndPoints
        {
            get
            {
                var i = 0;
                var result = new IPEndPoint[listeners.Count];
                foreach (var listener in listeners)
                {
                    result[i++] = (IPEndPoint)listener.LocalEndpoint;
                }
                return result;
            }
        }

        /// <summary>
        /// Stops listening and closes all client connections and the server.
        /// </summary>
        public void Close()
        {
            if (isClosed)
            {
                throw new ObjectDisposedException(nameof(SslServer));
            }

            isClosed = true;
            lock (listeners)
            {
                foreach (var listener in listeners)
                {
                    listener.Stop();
                }
                listeners.Clear();
            }
        }
        #endregion
    }
}
