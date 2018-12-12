using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Cave.Net;

namespace Cave.IO
{
    /// <summary>
    /// Provides <see cref="EventArgs"/> for <see cref="SslServer.Authenticate"/> events
    /// </summary>
    public class SslAuthenticationEventArgs : EventArgs
    {
        /// <summary>
        /// The local <see cref="SslClient"/> receiving the event
        /// </summary>
        public SslClient SslClient { get; private set; }

        /// <summary>
        /// The remote certificate to be authorized
        /// </summary>
        public X509Certificate2 Certificate { get; private set; }

        /// <summary>
        /// The remote certificate chain to be authorized
        /// </summary>
        public X509Chain Chain { get; private set; }

        /// <summary>
        /// The detected <see cref="SslPolicyErrors"/>
        /// </summary>
        public SslPolicyErrors SslPolicyErrors { get; private set; }

        /// <summary>
        /// The SSL validation errors
        /// </summary>
        public SslValidationErrors SslValidationErrors { get; private set; }

        bool m_Validated = true;

        /// <summary>
        /// Set this value to false to prohibit authorization. Setting this value to false does not allow to set it to true again!
        /// </summary>
        public bool Validated
        {
            get => m_Validated;
            set => m_Validated = m_Validated & value;
        }

        /// <summary>Creates a new SslAuthenticationEventArgs instance</summary>
        /// <param name="sslClient">The local <see cref="SslClient" /> receiving the event</param>
        /// <param name="certificate">The remote certificate to be authorized</param>
        /// <param name="chain">The remote certificate chain to be authorized</param>
        /// <param name="sslPolicyErrors">The detected <see cref="SslPolicyErrors" /></param>
        /// <param name="sslValidationErrors">The detected SSL validation errors.</param>
        public SslAuthenticationEventArgs(SslClient sslClient, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors, SslValidationErrors sslValidationErrors)
        {
            SslClient = sslClient;
            Certificate = certificate;
            Chain = chain;
            SslPolicyErrors = sslPolicyErrors;
            SslValidationErrors = sslValidationErrors;
        }
    }
}
