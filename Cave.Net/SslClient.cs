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

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Diagnostics;
using Cave.Net;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides a ssl client implementation 
    /// </summary>
    public class SslClient : IDisposable
    {
        #region private implementation
        SslStream m_Stream;
        TcpClient m_Client;
        IPEndPoint m_RemoteEndPoint;

        /// <summary>Called when [select local cert].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="targetHost">The target host.</param>
        /// <param name="localCertificates">The local certificates.</param>
        /// <param name="remoteCertificate">The remote certificate.</param>
        /// <param name="acceptableIssuers">The acceptable issuers.</param>
        /// <returns></returns>
        protected virtual X509Certificate OnSelectLocalCert(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            foreach (X509Certificate cert in localCertificates) return cert;
            return null;
        }

        /// <summary>Called when [validate remote cert].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns></returns>
        protected virtual bool OnValidateRemoteCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (PolicyErrors == 0) PolicyErrors = sslPolicyErrors;
            SslAuthenticationEventArgs e;
            if (certificate != null)
            {
                try
                {
                    string notAfterString = certificate.GetExpirationDateString();
                    DateTime notAfter = DateTime.Parse(notAfterString);
                    //DateTimeParser.ParseDateTime(notAfterString, out notAfter);

                    string notBeforeString = certificate.GetEffectiveDateString();
                    DateTime notBefore = DateTime.Parse(notBeforeString);
                    //DateTimeParser.ParseDateTime(notBeforeString, out notBefore);

                    if (DateTime.UtcNow < notBefore)
                    {
                        Trace.WriteLine($"CertificateError_NotBefore {certificate} {notBefore}");
                        ValidationErrors |= SslValidationErrors.NotJetValid;
                    }
                    if (DateTime.UtcNow > notAfter)
                    {
                        Trace.WriteLine($"CertificateError_NotAfter {certificate} {notAfter}");
                        ValidationErrors |= SslValidationErrors.NoLongerValid;
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"CertificateError_DateTime {certificate}");
                    return false;
                }
                e = new SslAuthenticationEventArgs(this, new X509Certificate2(certificate), chain, sslPolicyErrors, ValidationErrors);
            }
            else
            {
                e = new SslAuthenticationEventArgs(this, null, chain, sslPolicyErrors, ValidationErrors);
            }
            OnAuthenticate(e);
            return e.Validated;
        }

        #endregion

        /// <summary>
        /// This function will be called while authenticating a connection to another sslclient instance and runs the
        /// <see cref="Authenticate"/> event
        /// </summary>
        /// <param name="eventArgs"></param>
        protected virtual void OnAuthenticate(SslAuthenticationEventArgs eventArgs)
        {
            if (eventArgs == null) throw new ArgumentNullException("eventArgs");
            EventHandler<SslAuthenticationEventArgs> auth = Authenticate;
            if (auth != null)
            {
                auth.Invoke(this, eventArgs);
            }
            else
            {
                eventArgs.Validated &= (eventArgs.SslValidationErrors == SslValidationErrors.None);
                if (AllowClientAuthWithoutCert)
                {
                    eventArgs.Validated &= SslPolicyErrors.None == (eventArgs.SslPolicyErrors & ~SslPolicyErrors.RemoteCertificateNotAvailable);
                }
                else
                {
                    eventArgs.Validated &= SslPolicyErrors.None == eventArgs.SslPolicyErrors;
                }
            }
        }

        #region public events
        /// <summary>
        /// Event to be executed on each new incoming connection to be authenticated. The event may prohibit authentication
        /// based on the certificate, chain and errors encountered
        /// </summary>
        public event EventHandler<SslAuthenticationEventArgs> Authenticate;
        #endregion

        #region constructors
        /// <summary>
        /// Creates a new <see cref="SslClient"/> without certificate
        /// </summary>
        public SslClient() { }

        /// <summary>
        /// Creates a new <see cref="SslClient"/> without certificate
        /// </summary>
        public SslClient(TcpClient client)
        {
            if (client == null) throw new ArgumentNullException("client");
            m_Client = client;
            m_RemoteEndPoint = (IPEndPoint)m_Client.Client.RemoteEndPoint;
        }

        #endregion

        /// <summary>
        /// Check certificate revocation
        /// </summary>
        public bool CheckRevocation = false;

        /// <summary>Allow client authentication without cert</summary>
        public bool AllowClientAuthWithoutCert = true;

        /// <summary>
        /// Obtains the remote <see cref="IPEndPoint"/> this client is/was connected to
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get
            {
                return m_RemoteEndPoint;
            }
        }

        /// <summary>
        /// Checks whether the client is connected or not
        /// </summary>
        public bool Connected { get { return (m_Client != null) && (m_Client.Client.Connected); } }

        /// <summary>
        /// Starts TLS negotiation and authenticates as server. Use the Authenticate event to implement user defined policy checking!
        /// By default SslPolicyErrors will be ignored!
        /// </summary>
        public void DoServerTLS(X509Certificate2 certificate)
        {
            if (certificate == null) throw new ArgumentNullException("Certificate required!", "certificate");
            if (m_Stream != null) throw new InvalidOperationException(string.Format("TLS negotiation already started!"));
            if (m_Client == null) throw new InvalidOperationException(string.Format("Please establish connection first!"));
            if (!certificate.Verify()) throw new SecurityException("Certificate is invalid!");
            PolicyErrors = 0;
            ValidationErrors = 0;
            m_Stream = new SslStream(m_Client.GetStream(), false, new RemoteCertificateValidationCallback(OnValidateRemoteCert), new LocalCertificateSelectionCallback(OnSelectLocalCert));
            m_Stream.AuthenticateAsServer(certificate, false, SslProtocols.Tls, CheckRevocation);
            if (!m_Stream.IsEncrypted) throw new CryptographicException("Stream is not encrypted!");
            if (!m_Stream.IsAuthenticated) throw new CryptographicException("Stream is not authenticated!");
        }

        /// <summary>
        /// Starts TLS negotiation and authenticates as client. Use the Authenticate event to implement user defined policy checking!
        /// By default SslPolicyErrors will be ignored!
        /// </summary>
        /// <param name="serverCN"></param>
        public void DoClientTLS(string serverCN)
        {
            DoClientTLS(serverCN, null);
        }

        /// <summary>
        /// Starts TLS negotiation and authenticates as client. Use the Authenticate event to implement user defined policy checking!
        /// By default SslPolicyErrors will be ignored!
        /// </summary>
        /// <param name="serverCN">The servers common name (this is checked against the server certificate)</param>
        /// <param name="certificate">The clients certificate</param>
        public void DoClientTLS(string serverCN, X509Certificate2 certificate)
        {
            if (m_Stream != null) throw new InvalidOperationException(string.Format("TLS negotiation already started!"));
            if (m_Client == null) throw new InvalidOperationException(string.Format("Please establish connection first!"));
#if NET20 || NET35
			m_Stream = new SslStream(m_Client.GetStream(), false, new RemoteCertificateValidationCallback(OnValidateRemoteCert), new LocalCertificateSelectionCallback(OnSelectLocalCert));
#else
			m_Stream = new SslStream(m_Client.GetStream(), false, new RemoteCertificateValidationCallback(OnValidateRemoteCert), new LocalCertificateSelectionCallback(OnSelectLocalCert), EncryptionPolicy.RequireEncryption);
#endif
			X509CertificateCollection certificates = new X509CertificateCollection();
            if (certificate != null)
            {
                if (!certificate.Verify()) throw new SecurityException("Certificate is invalid!");
                certificates.Add(certificate);
            }
            m_Stream.AuthenticateAsClient(serverCN, certificates, SslProtocols.Tls, CheckRevocation);
			if (!m_Stream.IsEncrypted) throw new SecurityException("Stream is not encrypted!");
        }

        /// <summary>
        /// Creates a connection to the specified host and port
        /// </summary>
        /// <param name="host">The hostname or ipaddress</param>
        /// <param name="port">The port to connect to</param>
        public void Connect(string host, int port)
        {
            if (m_Client != null) throw new InvalidOperationException(string.Format("Connection already established!"));
            m_Client = new TcpClient(host, port);
            m_RemoteEndPoint = (IPEndPoint)m_Client.Client.RemoteEndPoint;
        }

        /// <summary>
        /// Creates a connection to the specified host and port
        /// </summary>
        /// <param name="address">The ipaddress</param>
        /// <param name="port">The port to connect to</param>
        public void Connect(IPAddress address, int port)
        {
            if (m_Client != null) throw new InvalidOperationException(string.Format("Connection already established!"));
            m_Client = new TcpClient();
            m_Client.Connect(address, port);
            m_RemoteEndPoint = (IPEndPoint)m_Client.Client.RemoteEndPoint;
        }

        /// <summary>
        /// Ontains the <see cref="Stream"/> instance for the client
        /// </summary>
        public Stream Stream
        {
            get
            {
                if (m_Stream == null) throw new InvalidOperationException(string.Format("TLS negotiation not jet started!"));
                return m_Stream;
            }
        }

        /// <summary>Gets the validation errors.</summary>
        /// <value>The validation errors.</value>
        public SslValidationErrors ValidationErrors { get; private set; }

        /// <summary>
        /// Obtains the policy errors found while authenticating
        /// </summary>
        public SslPolicyErrors PolicyErrors{ get; private set; }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName
        {
            get
            {
                if (Connected) return "SslClient <" + RemoteEndPoint + ">";
                return "SslClient <not connected>";
            }
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            if (m_Client != null)
            {
                m_Client.Close();
                m_Client = null;
            }
            if (m_Stream != null)
            {
                m_Stream.Close();
                m_Stream = null;
            }
        }

        /// <summary>
        /// Obtains an identification string for the object.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            result.Append("SSL://");
            if (RemoteEndPoint != null)
            {
                if (RemoteEndPoint.Address != IPAddress.Any)
                {
                    result.Append(RemoteEndPoint);
                }
                else
                {
                    result.Append(':');
                    result.Append(RemoteEndPoint.Port);
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Obtains a hashcode for this instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return RemoteEndPoint.GetHashCode();
        }

        /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // free managed resources
                if (m_Stream != null)
                {
                    m_Stream.Dispose();
                    m_Stream = null;
                }
                if (m_Client != null)
                {
                    ((IDisposable)m_Client).Dispose();
                    m_Client = null;
                }
            }
            // free native resources if there are any.
        }

        /// <summary>
        /// Releases all resources used by the this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
