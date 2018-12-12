using System;

namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides event arguments for <see cref="FtpServer.CheckAccess"/>
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public partial class FtpAccessEventArgs : EventArgs
    {
        bool denied;

        /// <summary>Initializes a new instance of the <see cref="FtpAccessEventArgs" /> class.</summary>
        /// <param name="client">The client.</param>
        /// <param name="accessType">Type of the access.</param>
        /// <param name="ftpName">Name of the FTP.</param>
        /// <param name="ftpFolder">The FTP folder.</param>
        /// <param name="fileSystemDirectory">The file system directory.</param>
        /// <param name="entries">The entries.</param>
        public FtpAccessEventArgs(FtpServerClient client, FtpAccessType accessType, string ftpName, string ftpFolder, string fileSystemDirectory, FtpDirectoryEntry[] entries)
        {
            Client = client;
            AccessType = accessType;
            FileSystemDirectory = fileSystemDirectory;
            FtpFolder = ftpFolder;
            FtpName = ftpName;
            Entries = entries;
        }

        /// <summary>Gets the type.</summary>
        /// <value>The type.</value>
        public FtpAccessType AccessType { get; }

        /// <summary>Gets the folder at the file system the client wants to access.</summary>
        public string FileSystemDirectory { get; }

        /// <summary>Gets the ftp folder the client wants to access.</summary>
        public string FtpFolder { get; }

        /// <summary>Gets the folder/filename at the ftp server the client wants to access.</summary>
        /// <value>The folder.</value>
        public string FtpName { get; }

        /// <summary>Gets the entries the client wants to access.</summary>
        /// <value>The entries.</value>
        /// <remarks>This property can be updated or filtered by the access handler</remarks>
        public FtpDirectoryEntry[] Entries { get; set; }

        /// <summary>Gets the client.</summary>
        /// <value>The client.</value>
        public FtpServerClient Client { get; }

        /// <summary>Gets or sets a value indicating whether the call is denied.</summary>
        /// <value><c>true</c> if denied; otherwise, <c>false</c>.</value>
        /// <remarks>You can set this only once to true. All further set commands will be ignored.</remarks>
        public bool Denied { get => denied; set => denied |= value; }
    }
}
