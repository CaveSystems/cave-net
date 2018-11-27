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

namespace Cave.Net
{
    /// <summary>
    /// Assigned Internet Protocol Numbers - Last Updated 2015-01-06
    /// http://www.iana.org/assignments/protocol-numbers/protocol-numbers.xhtml
    /// </summary>
    public enum IanaIcmpDestinationUnreachableCode
    {
        /// <summary>
        /// Network Unreachable
        /// </summary>
        Network = 0,

        /// <summary>
        /// Host Unreachable
        /// </summary>
        Host,

        /// <summary>
        /// Protocol Unreachable
        /// </summary>
        Protocol,
        /// <summary>
        /// Port Unreachable
        /// </summary>
        Port,
        /// <summary>
        /// Fragmentation Needed and Don't Fragment was Set
        /// </summary>
        Fragmentation,
        /// <summary>
        /// Source Route Failed
        /// </summary>
        SourceRoute,

        /// <summary>
        /// Destination Network Unknown
        /// </summary>
        DestinationNetworkUnknown,
        /// <summary>
        /// Destination Host Unknown
        /// </summary>
        DestinationHostUnknown,

        /// <summary>
        /// Source Host Isolated
        /// </summary>
        SourceHostIsolated,

        /// <summary>
        /// Communication with Destination Network is Administratively Prohibited
        /// </summary>
        NetworkProhibited,
        /// <summary>
        /// Communication with Destination Host is Administratively Prohibited
        /// </summary>
        HostProhibited,

        /// <summary>
        /// Destination Network Unreachable for Type of Service
        /// </summary>
        ServiceNetworkUnreachable,

        /// <summary>
        /// Destination Host Unreachable for Type of Service
        /// </summary>
        ServiceHostUnreachable,

        /// <summary>
        /// Communication Administratively Prohibited 
        /// </summary>
        CommunicationProhibited,

        /// <summary>
        /// Host Precedence Violation
        /// </summary>
        HostPrecedenceViolation,

        /// <summary>
        /// Precedence cutoff in effect
        /// </summary>
        PrecedenceCutoff,
    }
}
