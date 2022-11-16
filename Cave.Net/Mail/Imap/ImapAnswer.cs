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
using System.IO;
using System.Text;
using Cave.IO;

namespace Cave.Mail.Imap
{
    struct ImapAnswer
    {
        public string ID;

        public string Result;

        public bool Success => Result.StartsWith(ID + " OK");

        public byte[] Data;

        public string GetDataString() => ImapConst.ISO88591.GetString(Data);

        public string[] GetDataLines() => GetDataString().Split(new string[] { "\r\n" }, StringSplitOptions.None);

        /// <summary>
        /// Obtains a StreamReader for the current answer.
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public StreamReader GetStreamReader(long start)
        {
            if (start == 0)
            {
                return new StreamReader(new MemoryStream(Data), Encoding.ASCII);
            }
            else
            {
                return new StreamReader(new SubStream(new MemoryStream(Data), (int)start), Encoding.ASCII);
            }
        }

        /// <summary>
        /// Obtains a DataReader for the current answer.
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public DataReader GetDataReader(long start)
        {
            if (start == 0)
            {
                return new DataReader(new MemoryStream(Data), StringEncoding.ASCII);
            }
            else
            {
                return new DataReader(new SubStream(new MemoryStream(Data), (int)start), StringEncoding.ASCII);
            }
        }

        internal void Throw()
        {
            var index = Result.IndexOf(' ', ID.Length + 1) + 1;
            if (index <= 0)
            {
                throw new InvalidOperationException();
            }
            else
            {
                throw new InvalidOperationException(Result.Substring(index));
            }
        }

        /// <summary>
        /// Obtains the result.
        /// </summary>
        /// <returns></returns>
        public override string ToString() => Result;
    }
}
