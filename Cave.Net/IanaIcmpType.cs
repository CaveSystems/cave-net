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

namespace Cave.Net
{
    /// <summary>
    /// http://www.nthelp.com/icmp.html
    /// </summary>
    public enum IanaIcmpType
    {
        /// <summary>
        /// Echo Reply [RFC792]
        /// </summary>
        EchoReply = 0,

        /// <summary>
        /// Destination Unreachable [RFC792]
        /// </summary>
        DestinationUnreachable = 3,

        /// <summary>
        /// Source Quench [RFC792][RFC6633]
        /// </summary>
        [Obsolete("Deprecated")]
        SourceQuench = 4,

        /// <summary>
        /// Redirect [RFC792]
        /// </summary>
        Redirect = 5,

        /// <summary>
        /// Alternate Host Address [JBP][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        AlternateHostAddress = 6,

        /// <summary>
        /// Echo [RFC792]
        /// </summary>
        Echo = 8,

        /// <summary>
        /// Router Advertisement [RFC1256]
        /// </summary>
        RouterAdvertisement = 9,

        /// <summary>
        /// Router Selection [RFC1256]
        /// </summary>
        RouterSolicitation = 10,

        /// <summary>
        /// Time Exceeded [RFC792]
        /// </summary>
        TimeExceeded = 11,

        /// <summary>
        /// Parameter Problem [RFC792]
        /// </summary>
        ParameterProblem = 12,

        /// <summary>
        /// Timestamp [RFC792]
        /// </summary>
        Timestamp = 13,

        /// <summary>
        /// Timestamp Reply [RFC792]
        /// </summary>
        TimestampReply = 14,

        /// <summary>
        /// Information Request [RFC792][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        InformationRequest = 15,

        /// <summary>
        /// Information Reply [RFC792][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        Information_Reply = 16,

        /// <summary>
        /// Address Mask Request [RFC950][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        Address_Mask_Request = 17,

        /// <summary>
        /// Address Mask Reply [RFC950][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        Address_Mask_Reply = 18,

        /// <summary>
        /// Traceroute [RFC1393][RFC6918]
        /// </summary>
        Traceroute = 30,

        /// <summary>
        /// Datagram Conversion Error [RFC1475][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        DatagramConversionError = 31,

        /// <summary>
        /// Mobile Host Redirect [David_Johnson][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        MobileHostRedirect = 32,

        /// <summary>
        /// IPv6 Where-Are-You IPv6 Where-Are-You
        /// </summary>
        [Obsolete("Deprecated")]
        IPv6_WhereAreYou = 33,

        /// <summary>
        /// IPv6 I-Am-Here [Simpson][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        IPv6_IAmHere = 34,

        /// <summary>
        /// Mobile Registration Request [Simpson][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        MobileRegistrationRequest = 35,

        /// <summary>
        /// Mobile Registration Reply [Simpson][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        MobileRegistrationReply = 36,

        /// <summary>
        /// Domain Name Request [RFC1788][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        DomainNameRequest = 37,

        /// <summary>
        /// Domain Name Reply [RFC1788][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        DomainNameReply = 38,

        /// <summary>
        /// SKIP  [Markson][RFC6918]
        /// </summary>
        [Obsolete("Deprecated")]
        SKIP = 39,

        /// <summary>
        /// Photuris [RFC2521]
        /// </summary>
        RFC2521 = 40,

        /// <summary>
        /// ICMP messages utilized by experimental mobility protocols such as Seamoby [RFC4065]
        /// </summary>
        RFC4065 = 41,

        /// <summary>
        /// RFC3692-style Experiment 1 [RFC4727]
        /// </summary>
        RFC3692_style_Experiment_1 = 253,

        /// <summary>
        /// RFC3692-style Experiment 2 [RFC4727]
        /// </summary>
        RFC3692_style_Experiment_2 = 254,

        /// <summary>
        /// Reserved for future use
        /// </summary>
        Reserved = 255,
    }
}
