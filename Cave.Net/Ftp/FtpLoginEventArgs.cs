using System;

namespace Cave.Net.Ftp
{
    /// <summary>
    /// Provides event arguments for <see cref="FtpServer.CheckLogin"/>
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class FtpLoginEventArgs : EventArgs
    {
        bool denied;

        /// <summary>Initializes a new instance of the <see cref="FtpLoginEventArgs"/> class.</summary>
        /// <param name="client">The client.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="password">The password.</param>
        public FtpLoginEventArgs(FtpServerClient client, string userName, string password)
        {
            UserName = userName;
            Password = password;
            Client = client;
        }

        /// <summary>Gets the name of the user.</summary>
        /// <value>The name of the user.</value>
        public string UserName { get; }

        /// <summary>Gets the password.</summary>
        /// <value>The password.</value>
        public string Password { get; }

        /// <summary>Gets the client.</summary>
        /// <value>The client.</value>
        public FtpServerClient Client { get; }

        /// <summary>Gets or sets a value indicating whether the call is denied.</summary>
        /// <value><c>true</c> if denied; otherwise, <c>false</c>.</value>
        /// <remarks>You can set this only once to true. All further set commands will be ignored.</remarks>
        public bool Denied { get => denied; set => denied |= value; }
    }
}
