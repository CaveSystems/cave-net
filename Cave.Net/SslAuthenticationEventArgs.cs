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

using Cave.Net;
using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

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
            get { return m_Validated; }
            set { m_Validated = m_Validated & value; }
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
