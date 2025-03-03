using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Cave.Mail;

/// <summary>
/// Provides an <see cref="ICollection{Rfc822Message}"/> for <see cref="Rfc822Message"/> s This object directly works on the rfc822 header data of a message.
/// </summary>
public class Rfc822AddressCollection : ICollection<MailAddress>
{
    #region Private Fields

    readonly Encoding encoding;
    readonly NameValueCollection header;
    readonly string key;

    #endregion Private Fields

    #region Private Properties

    string Data
    {
        get => header[key]?.Trim() ?? string.Empty;
        set => header[key] = value;
    }

    #endregion Private Properties

    #region Private Methods

    List<MailAddress> Parse()
    {
        var result = new List<MailAddress>();
        foreach (var address in Data.Split(','))
        {
            if (address.Contains('@'))
            {
                result.Add(Rfc2047.DecodeMailAddress(address));
            }
        }
        return result;
    }

    void Write(List<MailAddress> addresses)
    {
        var data = new StringBuilder();
        var first = true;
        foreach (var address in addresses)
        {
            if (first) { first = false; }
            else { data.Append(", "); }
            data.Append(Rfc2047.EncodeMailAddress(TransferEncoding.QuotedPrintable, encoding, address));
        }
        Data = data.ToString();
    }

    #endregion Private Methods

    #region Internal Constructors

    internal Rfc822AddressCollection(string key, NameValueCollection header, Encoding encoding)
    {
        this.encoding = encoding;
        this.header = header;
        this.key = key;
        if (this.header[this.key] == null)
        {
            this.header[this.key] = "";
        }
    }

    internal Rfc822AddressCollection(string key) : this(key, new(), Encoding.ASCII) { }

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>Obtains the number of <see cref="MailAddress"/> present.</summary>
    public int Count => Parse().Count;

    /// <summary>returns always false.</summary>
    public bool IsReadOnly => false;

    #endregion Public Properties

    #region Public Methods

    /// <summary>Adds a <see cref="MailAddress"/>.</summary>
    /// <param name="item"></param>
    public void Add(MailAddress item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        var data = Data;
        if (!data.EndsWith(','))
        {
            data += ',';
        }
        data += ' ';
        data += item.ToString();
        Data = data;
    }

    /// <summary>Clears all addresses.</summary>
    public void Clear() => Data = "";

    /// <summary>Checks whether a specified <see cref="MailAddress"/> is part of the list.</summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Contains(MailAddress item) => Parse().Contains(item);

    /// <summary>Copies all <see cref="MailAddress"/> es to a specified array.</summary>
    /// <param name="array"></param>
    /// <param name="arrayIndex"></param>
    public void CopyTo(MailAddress[] array, int arrayIndex) => Parse().CopyTo(array, arrayIndex);

    /// <summary>Obtains a typed enumerator.</summary>
    /// <returns></returns>
    public IEnumerator<MailAddress> GetEnumerator() => Parse().GetEnumerator();

    /// <summary>Removes a <see cref="MailAddress"/> from the list.</summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public bool Remove(MailAddress item)
    {
        var result = Parse();
        if (result.Remove(item))
        {
            return false;
        }
        Write(result);
        return true;
    }

    /// <summary>Provides the header data.</summary>
    /// <returns></returns>
    public override string ToString() => key + ": " + Data;

    /// <summary>Obtains an untyped enumerator.</summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Parse()).GetEnumerator();

    #endregion Public Methods
}
