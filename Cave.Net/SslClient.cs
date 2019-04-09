using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides a ssl client implementation.
    /// </summary>
    public class SslClient : IDisposable
    {
        #region private implementation
        SslStream stream;
        TcpClient client;

        /// <summary>Called when [select local cert].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="targetHost">The target host.</param>
        /// <param name="localCertificates">The local certificates.</param>
        /// <param name="remoteCertificate">The remote certificate.</param>
        /// <param name="acceptableIssuers">The acceptable issuers.</param>
        /// <returns>Returns the selected <see cref="X509Certificate"/> instance.</returns>
        protected virtual X509Certificate OnSelectLocalCert(object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers)
        {
            foreach (X509Certificate cert in localCertificates)
            {
                return cert;
            }

            return null;
        }

        /// <summary>Called when [validate remote cert].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="certificate">The certificate.</param>
        /// <param name="chain">The chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns>Returns true if the remote certificate was validated or false otherwise.</returns>
        protected virtual bool OnValidateRemoteCert(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (PolicyErrors == 0)
            {
                PolicyErrors = sslPolicyErrors;
            }

            SslAuthenticationEventArgs e;
            if (certificate != null)
            {
                try
                {
                    string notAfterString = certificate.GetExpirationDateString();
                    var notAfter = DateTime.Parse(notAfterString);

                    // DateTimeParser.ParseDateTime(notAfterString, out notAfter);
                    string notBeforeString = certificate.GetEffectiveDateString();
                    var notBefore = DateTime.Parse(notBeforeString);

                    // DateTimeParser.ParseDateTime(notBeforeString, out notBefore);
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
                    Trace.TraceInformation($"CertificateError_DateTime {certificate}: {ex}");
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
        /// <see cref="Authenticate"/> event.
        /// </summary>
        /// <param name="eventArgs">Ssl authentication arguments.</param>
        protected virtual void OnAuthenticate(SslAuthenticationEventArgs eventArgs)
        {
            if (eventArgs == null)
            {
                throw new ArgumentNullException("eventArgs");
            }

            EventHandler<SslAuthenticationEventArgs> auth = Authenticate;
            if (auth != null)
            {
                auth.Invoke(this, eventArgs);
            }
            else
            {
                eventArgs.Validated &= eventArgs.SslValidationErrors == SslValidationErrors.None;
                if (AllowClientAuthWithoutCert)
                {
                    eventArgs.Validated &= (eventArgs.SslPolicyErrors & ~SslPolicyErrors.RemoteCertificateNotAvailable) == SslPolicyErrors.None;
                }
                else
                {
                    eventArgs.Validated &= eventArgs.SslPolicyErrors == SslPolicyErrors.None;
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
        /// Initializes a new instance of the <see cref="SslClient"/> class.
        /// </summary>
        public SslClient()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SslClient"/> class.
        /// </summary>
        /// <param name="client">Client to use.</param>
        public SslClient(TcpClient client)
        {
            this.client = client ?? throw new ArgumentNullException("client");
            RemoteEndPoint = (IPEndPoint)this.client.Client.RemoteEndPoint;
        }

        #endregion

        /// <summary>
        /// Check certificate revocation.
        /// </summary>
        public bool CheckRevocation = false;

        /// <summary>Allow client authentication without cert.</summary>
        public bool AllowClientAuthWithoutCert = true;

        /// <summary>
        /// Gets the remote <see cref="IPEndPoint"/> this client is/was connected to.
        /// </summary>
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the client is connected or not.
        /// </summary>
        public bool Connected => (client != null) && client.Client.Connected;

        /// <summary>
        /// Starts TLS negotiation and authenticates as server. Use the Authenticate event to implement user defined policy checking!
        /// By default SslPolicyErrors will be ignored.
        /// </summary>
        /// <param name="certificate">Certificate to use for the server instance.</param>
        public void DoServerTLS(X509Certificate2 certificate)
        {
            if (certificate == null)
            {
                throw new ArgumentNullException("Certificate required!", "certificate");
            }

            if (stream != null)
            {
                throw new InvalidOperationException(string.Format("TLS negotiation already started!"));
            }

            if (client == null)
            {
                throw new InvalidOperationException(string.Format("Please establish connection first!"));
            }

            if (!certificate.Verify())
            {
                throw new SecurityException("Certificate is invalid!");
            }

            PolicyErrors = 0;
            ValidationErrors = 0;
            stream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(OnValidateRemoteCert), new LocalCertificateSelectionCallback(OnSelectLocalCert));
            stream.AuthenticateAsServer(certificate, false, SslProtocols.Tls, CheckRevocation);
            if (!stream.IsEncrypted)
            {
                throw new CryptographicException("Stream is not encrypted!");
            }

            if (!stream.IsAuthenticated)
            {
                throw new CryptographicException("Stream is not authenticated!");
            }
        }

        /// <summary>
        /// Starts TLS negotiation and authenticates as client. Use the Authenticate event to implement user defined policy checking!
        /// By default SslPolicyErrors will be ignored.
        /// </summary>
        /// <param name="serverCN">Server common name (has to be present at the server certificate).</param>
        public void DoClientTLS(string serverCN)
        {
            DoClientTLS(serverCN, null);
        }

        /// <summary>
        /// Starts TLS negotiation and authenticates as client. Use the Authenticate event to implement user defined policy checking!
        /// By default SslPolicyErrors will be ignored.
        /// </summary>
        /// <param name="serverCN">The servers common name (this is checked against the server certificate).</param>
        /// <param name="certificate">The clients certificate.</param>
        public void DoClientTLS(string serverCN, X509Certificate2 certificate)
        {
            if (stream != null)
            {
                throw new InvalidOperationException(string.Format("TLS negotiation already started!"));
            }

            if (client == null)
            {
                throw new InvalidOperationException(string.Format("Please establish connection first!"));
            }
#if NET20 || NET35
            stream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(OnValidateRemoteCert), new LocalCertificateSelectionCallback(OnSelectLocalCert));
#else
            stream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(OnValidateRemoteCert), new LocalCertificateSelectionCallback(OnSelectLocalCert), EncryptionPolicy.RequireEncryption);
#endif
            var certificates = new X509CertificateCollection();
            if (certificate != null)
            {
                if (!certificate.Verify())
                {
                    throw new SecurityException("Certificate is invalid!");
                }

                certificates.Add(certificate);
            }
            stream.AuthenticateAsClient(serverCN, certificates, SslProtocols.Tls, CheckRevocation);
            if (!stream.IsEncrypted)
            {
                throw new SecurityException("Stream is not encrypted!");
            }
        }

        /// <summary>
        /// Creates a connection to the specified host and port.
        /// </summary>
        /// <param name="host">The hostname or ipaddress.</param>
        /// <param name="port">The port to connect to.</param>
        public void Connect(string host, int port)
        {
            if (client != null)
            {
                throw new InvalidOperationException(string.Format("Connection already established!"));
            }

            client = new TcpClient(host, port);
            RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
        }

        /// <summary>
        /// Creates a connection to the specified host and port.
        /// </summary>
        /// <param name="address">The ipaddress.</param>
        /// <param name="port">The port to connect to.</param>
        public void Connect(IPAddress address, int port)
        {
            if (client != null)
            {
                throw new InvalidOperationException(string.Format("Connection already established!"));
            }

            client = new TcpClient();
            client.Connect(address, port);
            RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint;
        }

        /// <summary>
        /// Gets the <see cref="Stream"/> instance for the client.
        /// </summary>
        public Stream Stream
        {
            get
            {
                if (stream == null)
                {
                    throw new InvalidOperationException(string.Format("TLS negotiation not jet started!"));
                }

                return stream;
            }
        }

        /// <summary>Gets the validation errors.</summary>
        /// <value>The validation errors.</value>
        public SslValidationErrors ValidationErrors { get; private set; }

        /// <summary>
        /// Gets the policy errors found while authenticating.
        /// </summary>
        public SslPolicyErrors PolicyErrors { get; private set; }

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName
        {
            get
            {
                return Connected ? $"SslClient <{RemoteEndPoint}>" : "SslClient <not connected>";
            }
        }

        /// <summary>
        /// Closes the connection.
        /// </summary>
        public void Close()
        {
            if (client != null)
            {
                client.Close();
                client = null;
            }
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        /// <summary>
        /// Obtains an identification string for the object.
        /// </summary>
        /// <returns>SSL://{RemoteEndPoint}.</returns>
        public override string ToString()
        {
            var result = new StringBuilder();
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
        /// Obtains a hashcode for this instance.
        /// </summary>
        /// <returns>Returns the hashcode for the remote endpoint.</returns>
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
                if (stream != null)
                {
                    stream.Dispose();
                    stream = null;
                }
                if (client != null)
                {
                    ((IDisposable)client).Dispose();
                    client = null;
                }
            }

            // free native resources if there are any.
        }

        /// <summary>
        /// Releases all resources used by the this instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
