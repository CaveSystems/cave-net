using System;

namespace Cave.Net
{
    /// <summary>Provides <see cref="EventArgs"/> for <see cref="SslClient"/> instances.</summary>
    public class SslClientEventArgs : EventArgs
    {
        #region Public Constructors

        /// <summary>Initializes a new instance of the <see cref="SslClientEventArgs"/> class.</summary>
        /// <param name="client">The SslClient.</param>
        public SslClientEventArgs(SslClient client) => Client = client;

        #endregion Public Constructors

        #region Public Properties

        /// <summary>Gets access to the client.</summary>
        public SslClient Client { get; private set; }

        #endregion Public Properties
    }
}
