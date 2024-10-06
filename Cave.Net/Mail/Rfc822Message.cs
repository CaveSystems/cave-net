using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Cave.IO;
using Cave.Security;

namespace Cave.Mail;

/// <summary>
/// Provides functions for rfc822 email parsing and processing. Multiple additions are made to accept malformed mail messages from providers like gmail
/// (encoding errors, empty multiparts), gmx (bad headers), ...
/// </summary>
public class Rfc822Message
{
    #region Private Fields

    static readonly char[] HeaderSeparator = [':'];
    byte[] body = [];
    int startOfBody;

    #endregion Private Fields

    #region Private Constructors

    Rfc822Message(byte[] data) => Parse(data);

    Rfc822Message(NameValueCollection header, byte[] data)
    {
        Parse(data);
        HeaderCollection = header;
        IsReadOnly = true;
    }

    #endregion Private Constructors

    #region Private Methods

    void Save(DataWriter writer)
    {
        //write header
        foreach (string key in HeaderCollection.Keys)
        {
            var values = GetHeaders(key);
            foreach (var value in values)
            {
                writer.Write(key);
                writer.Write(": ");
                if (ASCII.IsClean(value))
                {
                    writer.WriteLine(value);
                }
                else
                {
                    var str = Rfc2047.Encode(TransferEncoding.QuotedPrintable, Encoding.UTF8, value);
                    writer.WriteLine(str);
                }
            }
        }
        if (!IsMultipart)
        {
            writer.WriteLine();
            writer.Write(body);
        }
        else
        {
            //write main body
            writer.Write(body);
            //write additional parts
            var boundary = "--" + ContentType.Boundary;
            foreach (var part in Multipart)
            {
                writer.WriteLine();
                writer.WriteLine(boundary);
                part.Save(writer);
            }
            writer.WriteLine("--");
        }
    }

    #endregion Private Methods

    #region Protected Properties

    /// <summary>Gets all headers.</summary>
    protected NameValueCollection HeaderCollection { get; } = new();

    /// <summary>Gets a value indicating whether the message is readonly or not.</summary>
    protected bool IsReadOnly { get; }

    /// <summary>Gets all message parts</summary>
    protected List<Rfc822Message> Parts { get; } = new();

    #endregion Protected Properties

    #region Protected Methods

    /// <summary>Parses rfc822 data and fills the internal structures.</summary>
    /// <param name="data"></param>
    protected void Parse(byte[] data)
    {
        //reset
        Parts.Clear();
        HeaderCollection.Clear();
        //create reader
        var reader = new Rfc822Reader(data);
        //read header
        var line = Rfc2047.Decode(reader.ReadLine());
        //find first line
        while (string.IsNullOrEmpty(line))
        {
            line = Rfc2047.Decode(reader.ReadLine());
        }
        //read header
        while (!string.IsNullOrEmpty(line))
        {
            var header = line!;
            line = reader.ReadLine();
            //add folded content to current line
            while ((line != null) && (line.StartsWith(" ") || line.StartsWith("\t")))
            {
                header += " " + line.TrimStart(' ', '\t');
                line = reader.ReadLine();
            }
            //split "key: value" pair
            var splitPos = header.IndexOf(':');
            if (splitPos < 0)
            {
                break;
            }

            var headerKey = header[..splitPos];
            splitPos += 2;
            var headerVal = splitPos >= header.Length ? "" : header[splitPos..];
            try
            {
                Rfc2047.Decode(headerVal);
            }
            catch (Exception ex)
            {
                headerVal = Rfc2047.Encode(TransferEncoding.Base64, Encoding, headerVal);
                Trace.WriteLine($"Invalid header {headerKey}\n{ex}");
            }
            //add "key: value" pair to header
            HeaderCollection.Add(headerKey, headerVal);
        }
        //get body start position
        startOfBody = reader.Position;
        //multipart message ?
        if (!IsMultipart)
        {
            //no, single part
            body = reader.ReadToEndData();
        }
        else
        {
            //yes, multipart, get boundary
            MultiPartBoundary = "--" + ContentType.Boundary;
            //load body content without multiparts
            var endOfPart = reader.Position;
            line = reader.ReadLine();
            while (line != null)
            {
                if (line.StartsWith(MultiPartBoundary))
                {
                    break;
                }

                endOfPart = reader.Position;
                line = reader.ReadLine();
            }
            //decode part
            body = reader.Extract(startOfBody, endOfPart - startOfBody);

            //index start of current read part
            var startOfPart = reader.Position;
            //current read position (needed because multipart boundary does not belong to part content)
            var currentPosition = startOfPart;
            //load parts
            line = reader.ReadLine();

            var contentHeader = new NameValueCollection(HeaderCollection);
            var inHeader = true;
            while (line != null)
            {
                if (line is "." or "")
                {
                    inHeader = false;
                }
                else if (inHeader)
                {
                    var parts = line.Split(HeaderSeparator, 2);
                    if (parts.Length != 2)
                    {
                        inHeader = false;
                    }
                    else
                    {
                        contentHeader[parts[0]] = parts[1].TrimStart();
                    }
                }
                //part boundary detected ?
                if (line.StartsWith(MultiPartBoundary))
                {
                    //yes, get data of part as new buffer
                    var buffer = reader.Extract(startOfPart, currentPosition - startOfPart);
                    //decode part
                    Parts.Add(new(contentHeader, buffer));
                    //set next start position
                    startOfPart = reader.Position;
                    contentHeader = new(HeaderCollection);
                    inHeader = true;
                }
                currentPosition = reader.Position;
                line = reader.ReadLine();
            }
            //unclean message ending ? (missing "--\n" at end of file ?)
            if (currentPosition > (startOfPart + 3))
            {
                //yes, get data of part as new buffer
                var buffer = reader.Extract(startOfPart, currentPosition - startOfPart);
                //decode part
                Parts.Add(new(contentHeader, buffer));
            }
        }
    }

    #endregion Protected Methods

    #region Public Properties

    /// <summary>Accesses the 'Bcc:' Header field (BlindCarbonCopy).</summary>
    public Rfc822AddressCollection Bcc => new("Bcc", HeaderCollection, Encoding);

    /// <summary>Accesses the 'Cc:' Header field (CarbonCopy).</summary>
    public Rfc822AddressCollection Cc => new("Cc", HeaderCollection, Encoding);

    /// <summary>Gets / sets the content of the message.</summary>
    public string Content
    {
        get
        {
            try { return Rfc2047.DecodeText(TransferEncoding, Encoding, body); }
            catch { return Encoding.GetString(body); }
        }
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            body = Rfc2047.EncodeText(TransferEncoding, Encoding, value);
        }
    }

    /// <summary>Obtains the <see cref="ContentType"/>.</summary>
    public ContentType ContentType
    {
        get
        {
            var contentType = new ContentType
            {
                MediaType = "text/plain",
                CharSet = "iso-8859-1",
                Name = ""
            };
            var contentTypeString = HeaderCollection["Content-Type"];
            if (contentTypeString == null)
            {
                return contentType;
            }

            try
            {
                var parts = contentTypeString.Split(';');
                try { contentType.MediaType = parts[0].Trim().UnboxText(false).ToLowerInvariant().Replace(" ", ""); }
                catch { }
                foreach (var part in parts)
                {
                    var name = part.Trim().ToLowerInvariant();
                    var value = "";
                    var index = part.IndexOf('=');
                    if (index < 0)
                    {
                        continue;
                    }

                    value = part[(index + 1)..].Trim().UnboxText(false);
                    name = part[..index].Trim().ToLowerInvariant();
                    switch (name)
                    {
                        case "charset":
                            contentType.CharSet = value;
                            break;

                        case "boundary":
                            contentType.Boundary = value;
                            break;

                        case "name":
                            contentType.Name = value;
                            break;
                    }
                }
            }
            catch
            {
                //malformed content type
            }
            return contentType;
        }
    }

    /// <summary>Accesses the 'Date:' Header field.</summary>
    public DateTime Date
    {
        get => Rfc822DateTime.Decode(GetFirstHeader("Date"));
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            HeaderCollection["Date"] = Rfc822DateTime.Encode(value);
        }
    }

    /// <summary>Gets / sets the 'Delivered-To:' Header field.</summary>
    public MailAddress DeliveredTo
    {
        get => Rfc2047.DecodeMailAddress(GetFirstHeader("Delivered-To"));
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            HeaderCollection["Delivered-To"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
        }
    }

    /// <summary>Obtains the <see cref="System.Text.Encoding"/> used.</summary>
    public Encoding Encoding
    {
        get
        {
            try
            {
                var charSet = ContentType.CharSet;
                if (string.IsNullOrEmpty(charSet))
                {
                    return Encoding.GetEncoding("iso-8859-1");
                }

                return Encoding.GetEncoding(charSet.UnboxText(false));
            }
            catch
            {
                return Encoding.GetEncoding("iso-8859-1");
            }
        }
    }

    /// <summary>Gets / sets the 'From:' Header field.</summary>
    public MailAddress From
    {
        get => Rfc2047.DecodeMailAddress(GetFirstHeader("From"));
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            HeaderCollection["From"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
        }
    }

    /// <summary>Obtains whether the message contains at least one plain text part.</summary>
    public bool HasPlainTextPart => HasPart("text/plain");

    /// <summary>Obtains a copy of all header lines.</summary>
    public string[] Header
    {
        get
        {
            var result = new string[HeaderCollection.Count];
            var i = 0;
            foreach (string key in HeaderCollection.Keys)
            {
                result[i++] = key + ": " + HeaderCollection[key];
            }
            return result;
        }
    }

    /// <summary>Obtains whether the message is multipart or not.</summary>
    public bool IsMultipart
    {
        get
        {
            try { return !string.IsNullOrEmpty(ContentType.Boundary); }
            catch { return false; }
        }
    }

    /// <summary>Obtains whether the message looks valid or not.</summary>
    public bool IsValid => (HeaderCollection["From"] != null) && (HeaderCollection["To"] != null) && (HeaderCollection["Subject"] != null) && HasPlainTextPart;

    /// <summary>Gets / sets the 'Message-ID:' Header field.</summary>
    public MailAddress MessageID
    {
        get => Rfc2047.DecodeMailAddress(GetFirstHeader("Message-ID"));
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            HeaderCollection["Message-ID"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
        }
    }

    /// <summary>Gets / sets the 'MIME-Version:' Header field.</summary>
    public Version MimeVersion
    {
        get
        {
            try { return new(GetFirstHeader("MIME-Version")); }
            catch { return new("1.0"); }
        }
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            HeaderCollection["MIME-Version"] = value.Major + "." + value.Minor;
        }
    }

    /// <summary>Obtains all parts of the message. This can only be accessed after checking <see cref="IsMultipart"/>.</summary>
    public Rfc822MessageMultipart Multipart => new(Parts);

    /// <summary>Gets the multi part boundary.</summary>
    /// <value>The multi part boundary.</value>
    public string MultiPartBoundary { get; private set; } = $"--B{Environment.TickCount}";

    /// <summary>Gets / sets the 'Return-Path:' Header field.</summary>
    public MailAddress ReturnPath
    {
        get => Rfc2047.DecodeMailAddress(GetFirstHeader("Return-Path"));
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            HeaderCollection["Return-Path"] = Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, Encoding, value);
        }
    }

    /// <summary>Gets / sets the 'Subject:' Header field.</summary>
    public string Subject
    {
        get => Rfc2047.Decode(GetFirstHeader("Subject"));
        set
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException();
            }

            HeaderCollection["Subject"] = Rfc2047.Encode(TransferEncoding.QuotedPrintable, Encoding, value);
        }
    }

    /// <summary>Accesses the 'To:' Header field.</summary>
    public Rfc822AddressCollection To => new("To", HeaderCollection, Encoding);

    /// <summary>Obtains the <see cref="TransferEncoding"/>.</summary>
    public TransferEncoding TransferEncoding
    {
        get
        {
            //load transfer encoding
            var transferEncoding = HeaderCollection["Content-Transfer-Encoding"];
            if (transferEncoding == null)
            {
                return TransferEncoding.Unknown;
            }

            return transferEncoding.ToUpperInvariant() switch
            {
                "QUOTED-PRINTABLE" => TransferEncoding.QuotedPrintable,
                "BASE64" => TransferEncoding.Base64,
                "7BIT" => TransferEncoding.SevenBit,
                _ => TransferEncoding.Unknown
            };
        }
    }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Provides a new boundary string for multipart messages (the current boundary can be obtained via <see cref="ContentType"/>).</summary>
    /// <returns></returns>
    public static string CreateBoundary() => "_" + Rfc2047.GetRandomPrintableString(38) + "_";

    /// <summary>Reads a <see cref="Rfc822Message"/> from the specified binary data.</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Rfc822Message FromBinary(byte[] data) => new(data);

    /// <summary>Reads a <see cref="Rfc822Message"/> from the specified file.</summary>
    /// <param name="fileName"></param>
    /// <returns></returns>
    public static Rfc822Message FromFile(string fileName) => new(File.ReadAllBytes(fileName));

    /// <summary>Retrieve the first header field with the specified name.</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string GetFirstHeader(string key)
    {
        var result = HeaderCollection[key];
        if (result == null)
        {
            return "";
        }

        return result;
    }

    /// <summary>Obtains the hash code for the body of the message.</summary>
    /// <returns></returns>
    public override int GetHashCode() => body.GetHashCode();

    /// <summary>Retrieve all header fields with the specified name.</summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public string[] GetHeaders(string key) => HeaderCollection.GetValues(key) ?? [];

    /// <summary>Obtains the first part with the specified media type found in the message. This can only be accessed after checking <see cref="HasPart"/>.</summary>
    /// <returns></returns>
    public Rfc822Message GetPart(string mediaType)
    {
        if (IsMultipart)
        {
            if (HasPart(mediaType))
            {
                return Multipart.GetPart(mediaType);
            }
        }
        else if (string.Equals(ContentType.MediaType, mediaType))
        {
            return this;
        }

        throw new ArgumentException(string.Format("Message part {0} cannot be found!", mediaType));
    }

    /// <summary>Obtains the first plain text part found in the message. This can only be accessed after checking <see cref="HasPlainTextPart"/>.</summary>
    /// <returns></returns>
    public Rfc822Message GetPlainTextPart() => GetPart("text/plain");

    /// <summary>Obtains whether the message contains at least one part with the specified media type.</summary>
    public bool HasPart(string mediaType)
    {
        if (IsMultipart)
        {
            return Multipart.HasPart(mediaType);
        }

        return string.Equals(ContentType.MediaType, mediaType);
    }

    /// <summary>Saves a Rfc822 message to a file.</summary>
    /// <param name="fileName"></param>
    public void Save(string fileName)
    {
        using Stream stream = File.Create(fileName);
        var writer = new DataWriter(stream);
        Save(writer);
        writer.Close();
    }

    /// <summary>Obtains the ContentType of the Message as string.</summary>
    /// <returns></returns>
    public override string ToString() => Subject;

    #endregion Public Methods
}
