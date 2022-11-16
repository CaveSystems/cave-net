#region CopyRight 2018
/*
    Copyright (c) 2006-2018 Andreas Rohleder (andreas@rohleder.cc)
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
using System.Text;

namespace Cave.Mail.Imap
{
    /// <summary>
    /// Provides a search class for imap searches.
    /// </summary>
    public class ImapSearch
    {
        static string CheckString(string text)
        {
            if (Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(text)) != text)
            {
                throw new Exception("ImapSearch does not allow string searches with characters not part of US-ASCII !");
            }

            return text;
        }

        /// <summary>
        /// Searches all messages in the mailbox.
        /// </summary>
        public static ImapSearch ALL => new(ImapSearchType.ALL);

        /// <summary>
        /// Searches for messages that match all search keys.
        /// </summary>
        public static ImapSearch AND(params ImapSearch[] searches)
        {
            if (searches.Length < 1)
            {
                throw new Exception("ImapSearch.AND needs at least 1 search!");
            }

            var result = new StringBuilder();
            result.Append(searches[0]);
            for (var i = 1; i < searches.Length; i++)
            {
                result.Append(' ');
                result.Append(searches[i].ToString());
            }
            return new ImapSearch(ImapSearchType._MULTIPLE, result.ToString());
        }

        /// <summary>
        /// Searches messages with the \Answered flag set.
        /// </summary>
        public static ImapSearch ANSWERED => new(ImapSearchType.ANSWERED);

        /// <summary>
        /// Searches messages that contain the specified string in the envelope structure's BCC field.
        /// </summary>
        public static ImapSearch BCC => new(ImapSearchType.BCC);

        /// <summary>
        /// Searches messages whose internal date (disregarding time and timezone) is earlier than the specified date.
        /// </summary>
        public static ImapSearch BEFORE(DateTime dateTime) => new(ImapSearchType.BEFORE, dateTime.ToString("d-mmm-yyyy"));

        /// <summary>
        /// Searches messages that contain the specified string in the body of the message.
        /// </summary>
        public static ImapSearch BODY(string address) => new(ImapSearchType.BODY, CheckString(address));

        /// <summary>
        /// Searches messages that contain the specified string in the envelope structure's CC field.
        /// </summary>
        public static ImapSearch CC(string address) => new(ImapSearchType.CC, CheckString(address));

        /// <summary>
        /// Searches for messages with the \Deleted flag set.
        /// </summary>
        public static ImapSearch DELETED => new(ImapSearchType.DELETED);

        /// <summary>
        /// Searches for messages with the \Draft flag set.
        /// </summary>
        public static ImapSearch DRAFT => new(ImapSearchType.DRAFT);

        /// <summary>
        /// Searches for messages with the \Flagged flag set.
        /// </summary>
        public static ImapSearch FLAGGED => new(ImapSearchType.FLAGGED);

        /// <summary>
        /// Searches for messages that contain the specified string in the envelope structure's FROM field.
        /// </summary>
        public static ImapSearch FROM(string address) => new(ImapSearchType.FROM, CheckString(address));

        /// <summary>
        /// Searches for messages that have a header with the specified field-name (as defined in [RFC-2822]) and that contains the specified string in the text of the header (what comes after the colon). If the string to search is zero-length, this matches all messages that have a header line with the specified field-name regardless of the contents.
        /// </summary>
        public static ImapSearch HEADER(string fieldName, string text) => new(ImapSearchType.HEADER, CheckString(fieldName + " " + text));

        /// <summary>
        /// Searches for messages with the specified keyword flag set.
        /// </summary>
        public static ImapSearch KEYWORD(string flag) => new(ImapSearchType.KEYWORD, CheckString(flag));

        /// <summary>
        /// Searches for messages with an [RFC-2822] size larger than the specified number of octets.
        /// </summary>
        public static ImapSearch LARGER(int size) => new(ImapSearchType.LARGER, size.ToString());

        /// <summary>
        /// Searches for messages that have the \Recent flag set but not the \Seen flag. This is functionally equivalent to "(RECENT UNSEEN)".
        /// </summary>
        public static ImapSearch NEW => new(ImapSearchType.NEW);

        /// <summary>
        /// Searches messages that do not match the specified search key.
        /// </summary>
        public static ImapSearch NOT(ImapSearch search) => new(ImapSearchType.NOT, search.ToString());

        /// <summary>
        /// Searches for messages that do not have the \Recent flag set. This is functionally equivalent to "NOT RECENT" (as opposed to "NOT NEW").
        /// </summary>
        public static ImapSearch OLD => new(ImapSearchType.OLD);

        /// <summary>
        /// Searches for messages whose internal date (disregarding time and timezone) is within the specified date.
        /// </summary>
        public static ImapSearch ON(DateTime date) => new(ImapSearchType.ON, date.ToString("d-mmm-yyyy"));

        /// <summary>
        /// Searches for messages that match either search key.
        /// </summary>
        public static ImapSearch OR(params ImapSearch[] searches)
        {
            if (searches.Length < 2)
            {
                throw new Exception("ImapSearch.OR needs at least 2 searches!");
            }

            var result = new StringBuilder();
            result.Append(searches[0]);
            for (var i = 1; i < searches.Length; i++)
            {
                result.Append(" OR ");
                result.Append(searches[i].ToString());
            }
            return new ImapSearch(ImapSearchType._MULTIPLE, result.ToString());
        }

        /// <summary>
        /// Messages that have the \Recent flag set.
        /// </summary>
        public static ImapSearch RECENT => new(ImapSearchType.RECENT);

        /// <summary>
        /// Messages that have the \Seen flag set.
        /// </summary>
        public static ImapSearch SEEN => new(ImapSearchType.SEEN);

        /// <summary>
        /// Searches for messages whose [RFC-2822] Date: header (disregarding time and timezone) is earlier than the specified date.
        /// </summary>
        public static ImapSearch SENTBEFORE(DateTime date) => new(ImapSearchType.SENTBEFORE, date.ToString("d-mmm-yyyy"));

        /// <summary>
        /// Searches for messages whose [RFC-2822] Date: header (disregarding time and timezone) is within the specified date.
        /// </summary>
        public static ImapSearch SENTON(DateTime date) => new(ImapSearchType.SENTON, date.ToString("d-mmm-yyyy"));

        /// <summary>
        /// Searches for messages whose [RFC-2822] Date: header (disregarding time and timezone) is within or later than the specified date.
        /// </summary>
        public static ImapSearch SENTSINCE(DateTime date) => new(ImapSearchType.SENTSINCE, date.ToString("d-mmm-yyyy"));

        /// <summary>
        /// Searches for messages whose internal date (disregarding time and timezone) is within or later than the specified date.
        /// </summary>
        public static ImapSearch SINCE(DateTime date) => new(ImapSearchType.SINCE, date.ToString("d-mmm-yyyy"));

        /// <summary>
        /// Searches for messages with an [RFC-2822] size smaller than the specified number of octets.
        /// </summary>
        public static ImapSearch SMALLER(int size) => new(ImapSearchType.SMALLER, size.ToString());

        /// <summary>
        /// Searches for messages that contain the specified string in the envelope structure's SUBJECT field.
        /// </summary>
        public static ImapSearch SUBJECT(string text) => new(ImapSearchType.SUBJECT, CheckString(text));

        /// <summary>
        /// Searches for messages that contain the specified string in the header or body of the message.
        /// </summary>
        public static ImapSearch TEXT(string text) => new(ImapSearchType.TEXT, CheckString(text));

        /// <summary>
        /// Searches for messages that contain the specified string in the envelope structure's TO field.
        /// </summary>
        public static ImapSearch TO(string address) => new(ImapSearchType.TO, CheckString(address));

        /// <summary>
        /// Searches for messages with unique identifiers corresponding to the specified unique identifier set. Sequence set ranges are permitted.
        /// </summary>
        public static ImapSearch UID(uint uid) => new(ImapSearchType.UID, uid.ToString());

        /// <summary>
        /// Searches for messages with unique identifiers corresponding to the specified unique identifier set. Sequence set ranges are permitted.
        /// </summary>
        /// <param name="uidFirst"></param>
        /// <param name="uidLast"></param>
        /// <returns></returns>
        public static ImapSearch UID(uint uidFirst, int uidLast) => new(ImapSearchType.UID, uidFirst.ToString() + " " + uidLast.ToString());

        /// <summary>
        /// Searches messages that do not have the \Deleted flag set.
        /// </summary>
        public static ImapSearch UNANSWERED => new(ImapSearchType.UNANSWERED);

        /// <summary>
        /// Searches messages that do not have the \Deleted flag set.
        /// </summary>
        public static ImapSearch UNDELETED => new(ImapSearchType.UNDELETED);

        /// <summary>
        /// Searches messages that do not have the \Draft flag set.
        /// </summary>
        public static ImapSearch UNDRAFT => new(ImapSearchType.UNDRAFT);

        /// <summary>
        /// Searches messages that do not have the \Flagged flag set.
        /// </summary>
        public static ImapSearch UNFLAGGED => new(ImapSearchType.UNFLAGGED);

        /// <summary>
        /// Searches messages that do not have the specified keyword flag set.
        /// </summary>
        public static ImapSearch UNKEYWORD(string keyword)
        {
            if (string.IsNullOrEmpty(keyword))
            {
                throw new ArgumentNullException(nameof(keyword));
            }

            return new ImapSearch(ImapSearchType.UNKEYWORD, keyword);
        }

        /// <summary>
        /// Searches messages that do not have the \Seen flag set.
        /// </summary>
        public static ImapSearch UNSEEN => new(ImapSearchType.UNSEEN);

        readonly string text;

        private ImapSearch(ImapSearchType type)
            : this(type, null)
        { }

        private ImapSearch(ImapSearchType type, string parameters)
        {
            text = "";
            if (string.IsNullOrEmpty(parameters))
            {
                if (type == ImapSearchType._MULTIPLE)
                {
                    throw new InvalidOperationException();
                }

                text = type.ToString();
                return;
            }
            if (type == ImapSearchType._MULTIPLE)
            {
                text = parameters;
                return;
            }
            text = type.ToString() + " " + parameters;
        }

        /// <summary>
        /// Provides the searchtext.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => text;
    }
}
