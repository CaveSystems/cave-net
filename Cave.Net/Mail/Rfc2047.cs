using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using Cave.Net;

namespace Cave.Mail;

/// <summary>Provides en-/decoding routines for rfc2047 encoded strings.</summary>
public static class Rfc2047
{
    /// <summary>Obtains whether the specified string is rfc2047 encoded.</summary>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    public static bool IsEncodedString(string data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return !data.Contains(' ') && data.StartsWith("=?") && data.EndsWith("?=");
    }

    /// <summary>
    /// Encodes a text to quoted printable 7bit ascii data. The data is not split into parts of the correct length. The caller has to do
    /// this manually by inserting '=' + LF at the approprioate positions.
    /// </summary>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="text">The text to encode.</param>
    /// <returns></returns>
    public static byte[] EncodeQuotedPrintable(Encoding encoding, string text)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var result = new List<byte>();
        foreach (var ch in encoding.GetBytes(text))
        {
            if (ch is > 32 and < 128)
            {
                result.Add(ch);
                continue;
            }
            switch (ch)
            {
                case (byte)' ':
                    result.Add((byte)'_');
                    continue;
                case (byte)'_':
                case (byte)'=':
                case (byte)'?':
                default:
                    result.Add((byte)'=');
                    var hex = ch.ToString("X2");
                    result.Add((byte)hex[0]);
                    result.Add((byte)hex[1]);
                    continue;
            }
        }
        return result.ToArray();
    }

    /// <summary>Encodes text without start and end marks.</summary>
    /// <param name="transferEncoding">The transfer encoding to use.</param>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="text">The text to encode.</param>
    /// <returns></returns>
    public static byte[] EncodeText(TransferEncoding transferEncoding, Encoding encoding, string text)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        switch (transferEncoding)
        {
            case TransferEncoding.SevenBit:
                return ASCII.GetBytes(text);
            case TransferEncoding.Base64:
                var bytes = encoding.GetBytes(text);
                return ASCII.GetBytes(Base64.NoPadding.Encode(bytes));
            case TransferEncoding.QuotedPrintable:
                return EncodeQuotedPrintable(encoding, text);
            default:
                throw new InvalidDataException(string.Format("The specified encoding '{0}' is not valid for text!", transferEncoding.ToString()));
        }
    }

    /// <summary>Provides the default encoding of email content / header lines in quoted printable format.</summary>
    public static Encoding DefaultEncoding { get; private set; }

    /// <summary>Decodes a <see cref="MailAddress" />.</summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static MailAddress DecodeMailAddress(string data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var index = data.LastIndexOf('@');
        var recreate = (index == -1) || (index != data.IndexOf('@'));
        if (!recreate)
        {
            try { return new(Decode(data)); }
            catch { }
        }
        var cleanString = data.ReplaceInvalidChars(ASCII.Strings.SafeName, " ");
        return new(GetRandomPrintableString(20) + "@" + NetTools.HostName, cleanString);
    }

    /// <summary>Obtains a random printable string of the specified length.</summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public static string GetRandomPrintableString(int count)
    {
        var random = new Random();
        var buffer = new char[count];
        for (var i = 0; i < count; i++)
        {
            buffer[i] = (char)random.Next(33, 127);
        }

        return new(buffer);
    }

    /// <summary>Decodes quoted printable 7bit ascii data to the specified <see cref="Encoding" />.</summary>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    public static string DecodeQuotedPrintable(Encoding encoding, byte[] data) => DecodeQuotedPrintable(encoding, ASCII.GetString(data));

    /// <summary>Decodes quoted printable 7bit ascii data to the specified <see cref="Encoding" />.</summary>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    public static string DecodeQuotedPrintable(Encoding encoding, string data)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        var result = new List<byte>(data.Length);
        for (var i = 0; i < data.Length; i++)
        {
            int ch = data[i];

            if (ch > 127)
            {
                throw new FormatException(string.Format("Invalid input character at position '{0}' encountered!", i));
            }
            //got '_' == encoded space ?
            if (ch == '_')
            {
                result.AddRange(encoding.GetBytes(" "));
                continue;
            }
            //got '=' == start of 2 digit hex value
            if (ch == '=')
            {
                i += 1;
                //got line extension ?
                if (data[i] == '\r')
                {
                    if (data[i + 1] == '\n')
                    {
                        i++;
                    }

                    continue;
                }
                if (data[i] == '\n')
                {
                    continue;
                }
                //no line extension, decode hex value
                var hexValue = data.Substring(i, 2);
                var value = Convert.ToInt32(hexValue, 16);
                result.Add((byte)value);
                i += 1;
                continue;
            }
            result.AddRange(encoding.GetBytes(((char)ch).ToString()));
        }
        return encoding.GetString(result.ToArray());
    }

    /// <summary>Decodes ascii 7bit data using the specified <see cref="TransferEncoding" />.</summary>
    /// <param name="transferEncoding">The transfer encoding to use.</param>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    public static string DecodeText(TransferEncoding transferEncoding, Encoding encoding, string data)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        return transferEncoding switch
        {
            TransferEncoding.Base64 => encoding.GetString(Convert.FromBase64String(data.RemoveNewLine())), //convert: byte array -> base64 ascii string -> raw data -> string
            TransferEncoding.QuotedPrintable => DecodeQuotedPrintable(encoding, data),
            TransferEncoding.SevenBit => encoding.GetString(ASCII.GetBytes(data)),
            _ => throw new InvalidDataException(string.Format("The specified encoding '{0}' is not valid for text parts!", transferEncoding.ToString()))
        };
    }

    /// <summary>Decodes ascii 7bit data using the specified <see cref="TransferEncoding" />.</summary>
    /// <param name="transferEncoding">The transfer encoding to use.</param>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    public static string DecodeText(TransferEncoding transferEncoding, Encoding encoding, byte[] data)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (transferEncoding == TransferEncoding.Unknown)
        {
            return encoding.GetString(data);
        }

        return DecodeText(transferEncoding, encoding, ASCII.GetString(data));
    }

    /// <summary>
    /// Decodes a rfc2047 string with correct start and end marks. If a header line needs to be decoded can be tested with
    /// <see cref="IsEncodedString" />.
    /// </summary>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    public static string Decode(byte[] data) => Decode(ASCII.GetString(data));

    /// <summary>Decodes multiple <see cref="MailAddress" />es.</summary>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    public static string Decode(string data)
    {
        if (data == null)
        {
            return null;
        }

        var start = data.IndexOf("=?");
        var end = IndexOfEndMark(data, start);
        if (start >= end)
        {
            return data;
        }

        var result = new StringBuilder(data.Length);
        var pos = 0;
        int size;
        while ((start > -1) && (start < end))
        {
            //copy text without decoding ?
            size = start - pos;
            if (size > 0)
            {
                result.Append(data.Substring(pos, size));
            }
            //decode
            size = (end - start) + 2;
            result.Append(DecodeInternal(data.Substring(start, size)));
            pos = end + 2;
            start = data.IndexOf("=?", pos);
            end = IndexOfEndMark(data, start);
        }
        size = data.Length - pos;
        if (size > 0)
        {
            result.Append(data.Substring(pos, size));
        }

        return result.ToString();
    }

    /// <summary>Encodes a string to a valid rfc2047 string with the specified <see cref="TransferEncoding" />.</summary>
    /// <param name="transferEncoding">The transfer encoding to use.</param>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="text">The string to encode.</param>
    /// <returns></returns>
    public static string Encode(TransferEncoding transferEncoding, Encoding encoding, string text)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }
        //TODO BREAK LINE AFTER 76 CHARS, NEXT LINE STARTS WITH \t
        return transferEncoding switch
        {
            TransferEncoding.QuotedPrintable => "=?" + encoding.WebName + "?Q?" + ASCII.GetString(EncodeQuotedPrintable(encoding, text)) + "?=",
            TransferEncoding.Base64 => "=?" + encoding.WebName + "?B?" + Base64.NoPadding.Encode(encoding.GetBytes(text)) + "?=",
            _ => throw new InvalidDataException(string.Format("The specified encoding '{0}' is not valid for text!", transferEncoding.ToString()))
        };
    }

    /// <summary>Encodes a <see cref="MailAddress" />.</summary>
    /// <param name="transferEncoding">The transfer encoding to use.</param>
    /// <param name="encoding">The binary encoding to use.</param>
    /// <param name="address">The address to encode.</param>
    /// <returns></returns>
    public static string EncodeMailAddress(TransferEncoding transferEncoding, Encoding encoding, MailAddress address)
    {
        if (encoding == null)
        {
            throw new ArgumentNullException(nameof(encoding));
        }

        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (string.IsNullOrEmpty(address.DisplayName))
        {
            return address.Address;
        }
        return "\"" + Encode(transferEncoding, encoding, address.DisplayName) + "\" <" + address.Address + ">";
    }

    static int IndexOfEndMark(string text, int start)
    {
        if (start < 0)
        {
            return -1;
        }

        var markers = 4;
        for (var i = start; i < text.Length; i++)
        {
            if (text[i] == '?')
            {
                if (--markers == 0)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    /// <summary>
    /// Decodes a rfc2047 string with correct start and end marks. If a header line needs to be decoded can be tested with
    /// <see cref="IsEncodedString" />.
    /// </summary>
    /// <param name="data">TransferEncoded ascii data.</param>
    /// <returns></returns>
    static string DecodeInternal(string data)
    {
        if (IsEncodedString(data))
        {
            var encoded = data.Substring(2, data.Length - 4);
            var parts = new string[3];
            var part = 0;
            var start = 0;
            for (var i = 0; i < encoded.Length; i++)
            {
                if (encoded[i] == '?')
                {
                    parts[part++] = encoded.Substring(start, i - start);
                    start = ++i;
                    if (part == 2)
                    {
                        break;
                    }
                }
            }
            parts[2] = encoded.Substring(start, encoded.Length - start);
            //load default encoding used as fallback
            var encoding = Encoding.GetEncoding(1252);
            //try to get encoding by webname, many non standard email services use whatever they want as encoding string
            try { encoding = Encoding.GetEncoding(parts[0].UnboxText(false)); }
            catch
            {
                //.. so we get in one of 10 emails no valid encoding here. maybe they used the codepage prefixed or suffixed with a string:
                try { encoding = Encoding.GetEncoding(int.Parse(parts[0].GetValidChars(ASCII.Strings.Digits))); }
                catch
                {
                    /* no they didn't, so we use the default and hope the best... */
                }
            }
            try
            {
                return parts[1].ToUpperInvariant() switch
                {
                    "B" => encoding.GetString(Convert.FromBase64String(parts[2])),
                    "Q" => DecodeQuotedPrintable(encoding, parts[2]),
                    _ => throw new InvalidDataException(string.Format("Unknown encoding '{0}'!", parts[1]))
                };
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Invalid data encountered!", ex);
            }
        }
        return data;
    }

    static Rfc2047() => DefaultEncoding = Encoding.GetEncoding(1252);
}
