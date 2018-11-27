#region CopyRight 2018
/*
    Copyright (c) 2007-2018 Andreas Rohleder (andreas@rohleder.cc)
    All rights reserved
*/
#endregion
#region License LGPL-3
/*
    This program/library/sourcecode is free software; you can redistribute it
    and/or modify it under the terms of the GNU Lesser General Public License
    version 3 as published by the Free Software Foundation subsequent called
    the License.

    You may not use this program/library/sourcecode except in compliance
    with the License. The License is included in the LICENSE file
    found at the installation directory or the distribution package.

    Permission is hereby granted, free of charge, to any person obtaining
    a copy of this software and associated documentation files (the
    "Software"), to deal in the Software without restriction, including
    without limitation the rights to use, copy, modify, merge, publish,
    distribute, sublicense, and/or sell copies of the Software, and to
    permit persons to whom the Software is furnished to do so, subject to
    the following conditions:

    The above copyright notice and this permission notice shall be included
    in all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
    NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
    LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
    OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
    WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion
#region Authors & Contributors
/*
   Author:
     Andreas Rohleder <andreas@rohleder.cc>

   Contributors:
 */
#endregion

using Cave.Collections.Generic;
using Cave.Text;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Diagnostics;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides a ssl server implementation accepting and authenticating <see cref="SslClient"/> connections
    /// </summary>
    public class SslServer
    {
        #region private implementation
        readonly List<TcpListener> m_Listeners = new List<TcpListener>();
        bool m_Closed = false;

        void m_Listen(object obj)
        {
            TcpListener listener = obj as TcpListener;
            listener.Start();
            while (!m_Closed)
            {
                //stop if listener closed
                lock (m_Listeners) if (!m_Listeners.Contains(listener)) break;
                //accept client
                TcpClient tcpClient = null;
                try
                {
                    tcpClient = listener.AcceptTcpClient();
                    SslClient l_SslClient = new SslClient(tcpClient);
                    l_SslClient.Authenticate += new EventHandler<SslAuthenticationEventArgs>(m_ClientAuthenticate);
                    l_SslClient.DoServerTLS(Certificate);
                    OnConnected(l_SslClient);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    try { if ((tcpClient != null) && tcpClient.Connected) tcpClient.Close(); }
                    catch { }
                }
            }
        }

        void m_ClientAuthenticate(object sender, SslAuthenticationEventArgs e)
        {
            OnAuthenticate(e);
        }

        #endregion

        #region event starters

        /// <summary>
        /// Handles the incoming connection and calls the <see cref="Connected"/> event
        /// </summary>
        /// <param name="client"></param>
        protected virtual void OnConnected(SslClient client)
        {
            EventHandler<SslClientEventArgs> evt = Connected;
            //return if event not used
            try
            {
                evt?.Invoke(this, new SslClientEventArgs(client));
            }
            catch (Exception ex)
            {
                if (client.Connected)
                {
                    try { client.Close(); }
                    catch { }
                }
                Trace.WriteLine(ex);
            }
        }

        /// <summary>
        /// Calls the <see cref="Authenticate"/> event
        /// </summary>
        /// <param name="eventArgs"></param>
        protected virtual void OnAuthenticate(SslAuthenticationEventArgs eventArgs)
        {
            EventHandler<SslAuthenticationEventArgs> evt = Authenticate;
            if (evt != null) evt.Invoke(this, eventArgs);
        }
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
        /// The certificate the server uses
        /// </summary>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// Creates a new SslServer with the specified certificate
        /// </summary>
        /// <param name="certificate">The certificate</param>
        public SslServer(X509Certificate2 certificate)
        {
            Certificate = certificate as X509Certificate2;
            if (Certificate == null) throw new ArgumentException(string.Format("Certificate has to be a valid X509Certificate2!"));
        }

        /// <summary>
        /// Starts listening at the specified port (IPv4)
        /// </summary>
        /// <param name="address">The ip address to listen at</param>
        /// <param name="port">The port (1..65534) to listen at</param>
        public void Listen(IPAddress address, int port)
        {
            if (m_Closed) throw new ObjectDisposedException("SslServer");
            TcpListener listener = new TcpListener(address, port);
            listener.ExclusiveAddressUse = true;
            Task.Factory.StartNew(new Action<object>(m_Listen), listener);
            m_Listeners.Add(listener);
        }

        /// <summary>
        /// Starts listening at the specified local <see cref="IPEndPoint"/>
        /// </summary>
        /// <param name="iPEndPoint">The local IPEndPoint to listen at</param>
        public void Listen(IPEndPoint iPEndPoint)
        {
            if (m_Closed) throw new ObjectDisposedException("SslServer");
            TcpListener listener = new TcpListener(iPEndPoint);
            listener.ExclusiveAddressUse = true;
            Task.Factory.StartNew(new Action<object>(m_Listen), listener);
            m_Listeners.Add(listener);
        }

        /// <summary>
        /// Provides a list of local IPEndPoints currently listened at
        /// </summary>
        public IPEndPoint[] LocalEndPoints
        {
            get
            {
                int i = 0;
                IPEndPoint[] result = new IPEndPoint[m_Listeners.Count];
                foreach (TcpListener listener in m_Listeners)
                {
                    result[i++] = (IPEndPoint)listener.LocalEndpoint;
                }
                return result;
            }
        }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName { get { return "SslServer"; } }

        /// <summary>
        /// Stops listening and closes all client connections and the server
        /// </summary>
        public void Close()
        {
            if (m_Closed) throw new ObjectDisposedException("SslServer");
            m_Closed = true;
            lock (m_Listeners)
            {
                foreach (TcpListener listener in m_Listeners)
                {
                    listener.Stop();
                }
                m_Listeners.Clear();
            }
        }
        #endregion
    }
}
