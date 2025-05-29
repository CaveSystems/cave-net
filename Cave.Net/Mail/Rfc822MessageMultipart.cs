using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Mime;

namespace Cave.Mail;

/// <summary>Provides <see cref="Rfc822Message"/> accessor for multipart messages.</summary>
public class Rfc822MessageMultipart : IEnumerable<Rfc822Message>
{
    #region Private Fields

    readonly List<Rfc822Message> parts;

    #endregion Private Fields

    #region Private Methods

    /// <summary>Gets the <see cref="ContentTypes"/> in this multipart message. This does not traverse into nested multiparts !.</summary>
    /// <returns></returns>
    List<ContentType> GetContentTypes()
    {
        var result = new List<ContentType>();
        foreach (var part in parts)
        {
            result.Add(part.ContentType);
        }
        return result;
    }

    #endregion Private Methods

    #region Internal Constructors

    internal Rfc822MessageMultipart(List<Rfc822Message> items) => parts = items;

    #endregion Internal Constructors

    #region Public Properties

    /// <summary>Obtains all <see cref="ContentType"/> s found. This does not traverse into nested multiparts !.</summary>
    public ContentType[] ContentTypes => GetContentTypes().ToArray();

    /// <summary>Obtains the number of parts.</summary>
    public int Count => parts.Count;

    #endregion Public Properties

    #region Public Indexers

    /// <summary>Obtains the part with the specified <see cref="ContentType"/>. This does not traverse into nested multiparts !.</summary>
    /// <param name="contentType"></param>
    /// <returns></returns>
    public Rfc822Message this[ContentType contentType]
    {
        get
        {
            if (contentType == null)
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            return this[contentType.MediaType];
        }
    }

    /// <summary>Obtains the first part found with the specified MediaType (e.g. text/plain). This does not traverse into nested multiparts !.</summary>
    /// <param name="mediaType"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Thrown if the media type cannot be found.</exception>
    public Rfc822Message this[string mediaType]
    {
        get
        {
            mediaType = mediaType.GetValidChars("abcdefghijklmnopqrstuvwxyz/");
            var contentTypes = ContentTypes;
            for (var i = 0; i < contentTypes.Length; i++)
            {
                var other = contentTypes[i].MediaType.ToUpperInvariant().GetValidChars("abcdefghijklmnopqrstuvwxyz/");
                if (mediaType == other)
                {
                    return this[i];
                }
            }
            throw new ArgumentException(string.Format("ContentType {0} not found!", mediaType));
        }
    }

    /// <summary>Obtains the part with the specified index.</summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Rfc822Message this[int index] => parts[index];

    #endregion Public Indexers

    #region Public Methods

    /// <summary>Obtains a Rfc822Message enumerator for all parts.</summary>
    /// <returns></returns>
    public IEnumerator<Rfc822Message> GetEnumerator() => parts.GetEnumerator();

    /// <summary>Obtains the first plain text part found in the message. This can only be accessed after checking <see cref="HasPart"/>.</summary>
    /// <returns></returns>
    public Rfc822Message GetPart(string mediaType)
    {
        foreach (var part in parts)
        {
            if (part.HasPart(mediaType))
            {
                return part.GetPart(mediaType);
            }
        }
        throw new ArgumentException(string.Format("Message part {0} cannot be found!", mediaType));
    }

    /// <summary>Obtains whether the message contains at least one plain text part.</summary>
    public bool HasPart(string mediaType)
    {
        foreach (var part in parts)
        {
            if (part.HasPart(mediaType))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Obtains a Rfc822Message enumerator for all parts.</summary>
    /// <returns></returns>
    IEnumerator IEnumerable.GetEnumerator() => parts.GetEnumerator();

    #endregion Public Methods
}
