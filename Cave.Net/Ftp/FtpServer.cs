using System;
using Cave.Collections.Generic;
using Cave.IO;

namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides a ftp server implementation
    /// </summary>
    /// <seealso cref="TcpServer{FtpServerClient}" />
    /// <remarks>
    /// RFC 3659 - Extensions to FTP
    /// RFC 2640 - Internationalization of the File Transfer Protocol
    /// RFC 2389 - Feature negotiation mechanism for the File Transfer Protocol
    /// RFC 2228 - FTP Security Extensions
    /// RFC 959 - File Transfer Protocol
    /// </remarks>
    public class FtpServer : TcpServer<FtpServerClient>
    {
        /// <summary>Gets or sets the default encoding for old or non standard ftp clients like microsoft windows.</summary>
        /// <value>The default encoding.</value>
        public StringEncoding DefaultEncoding { get; set; } = StringEncoding.ISO_8859_2;

        /// <summary>Raises the <see cref="E:CheckAccess" /> event.</summary>
        /// <param name="e">The <see cref="FtpLoginEventArgs"/> instance containing the event data.</param>
        protected internal virtual void OnCheckLogin(FtpLoginEventArgs e)
        {
            CheckLogin?.Invoke(this, e);
        }

        /// <summary>Raises the <see cref="E:CheckAccess" /> event.</summary>
        /// <param name="e">The <see cref="FtpAccessEventArgs"/> instance containing the event data.</param>
        protected internal virtual void OnCheckAccess(FtpAccessEventArgs e)
        {
            CheckAccess?.Invoke(this, e);
        }

        /// <summary>Gets the root folders.</summary>
        /// <value>The root folders.</value>
        public SynchronizedDictionary<string, string> RootFolders { get; } = new SynchronizedDictionary<string, string>();

        #region events
        /// <summary>Occurs when a user tries a login.</summary>
        public event EventHandler<FtpLoginEventArgs> CheckLogin;

        /// <summary>Occurs when a user tries to access a file/directory.</summary>
        public event EventHandler<FtpAccessEventArgs> CheckAccess;
        #endregion

        /// <summary>Gets the server version string.</summary>
        /// <value>The server version string.</value>
        public string ServerVersionString => $"{typeof(FtpServer).Name}/{AssemblyVersionInfo.Program} {Base32.Safe.Encode(AppDom.ProgramID)} ({Platform.Type}; {Platform.SystemVersionString}) .NET/{Environment.Version}";
    }
}
