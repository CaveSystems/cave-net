using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Cave.IO;

namespace Cave.Net;

/// <summary>Provides domain name parsing.</summary>
public sealed class DomainName
{
    #region Private Fields

    static readonly Regex AsciiNameRegex = new("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    static readonly string[] Empty = [];
    static readonly IdnMapping IdnParser = new() { UseStd3AsciiRules = true };

    #endregion Private Fields

    #region Private Methods

    static bool TryParsePart(string value, out string label)
    {
        try
        {
            if (AsciiNameRegex.IsMatch(value))
            {
                label = value;
                return true;
            }
            label = IdnParser.GetAscii(value);
            return true;
        }
        catch
        {
            label = string.Empty;
            return false;
        }
    }

    #endregion Private Methods

    #region Public Fields

    /// <summary>Provides the characters safe for domain names.</summary>
    public const string SafeChars = ASCII.Strings.Letters + ASCII.Strings.Digits + "_-";

    #endregion Public Fields

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="DomainName"/> class.</summary>
    /// <param name="parts">The parts of the DomainName.</param>
    public DomainName(IEnumerable<string> parts)
        : this(parts?.ToArray()) { }

    /// <summary>Initializes a new instance of the <see cref="DomainName"/> class.</summary>
    /// <param name="parts">The parts of the DomainName.</param>
    public DomainName(params string[]? parts)
    {
        Parts = parts ?? Empty;
        foreach (var part in Parts)
        {
            if (part.HasInvalidChars(SafeChars))
            {
                throw new InvalidDataException("Invalid characters at domain name!");
            }
        }
    }

    #endregion Public Constructors

    #region Public Properties

    /// <summary>Gets the DNS root name (.)</summary>
    /// <value>The root.</value>
    public static DomainName Root { get; } = new(null);

    /// <summary>Gets the parts of the domain name.</summary>
    /// <value>The parts.</value>
    public string[] Parts { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Performs an implicit conversion from <see cref="string"/> to <see cref="DomainName"/>.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The result of the conversion.</returns>
    /// <exception cref="InvalidDataException">{0} is not a valid domain name.</exception>
    public static implicit operator DomainName(string value) =>
        (TryParse(value, out var name) ? name : null) ?? throw new InvalidDataException(string.Format("{0} is not a valid domain name!", value));

    /// <summary>Implements the operator !=.</summary>
    /// <param name="a">a.</param>
    /// <param name="b">The b.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(DomainName a, DomainName b) => b is null ? a is not null : a is null || (a.ToString() != b.ToString());

    /// <summary>Implements the operator ==.</summary>
    /// <param name="a">a.</param>
    /// <param name="b">The b.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(DomainName a, DomainName b) => b is null ? a is null : a is not null && (a.ToString() == b.ToString());

    /// <summary>Parses a domain name using the specified reader.</summary>
    /// <param name="reader">The reader.</param>
    /// <returns>Returns a new <see cref="DomainName"/> instance.</returns>
    /// <exception cref="NotSupportedException">Unsupported extended dns label.</exception>
    public static DomainName Parse(DataReader reader)
    {
        long endposition = -1;
        var parts = new List<string>();
        while (true)
        {
            var b = reader.ReadByte();
            if (b == 0)
            {
                // end of domain, RFC1035
                if (endposition > 0)
                {
                    reader.BaseStream.Position = endposition;
                }

                return new([.. parts]);
            }
            if (b >= 192)
            {
                // Pointer, RFC1035
                var pointer = ((b - 192) * 256) + reader.ReadByte();

                // save position
                if (endposition < 0)
                {
                    endposition = reader.BaseStream.Position;
                }

                reader.BaseStream.Position = pointer;
                continue;
            }
            if (b == 65)
            {
                // binary EDNS label, RFC2673, RFC3363, RFC3364
                int length = reader.ReadByte();
                if (length == 0)
                {
                    length = 256;
                }

                var sb = new StringBuilder();
                sb.Append(@"\[x");
                var suffix = "/" + length + "]";
                do
                {
                    b = reader.ReadByte();
                    if (length < 8)
                    {
                        b &= (byte)(0xff >> (8 - length));
                    }
                    sb.Append(b.ToString("x2"));
                    length -= 8;
                }
                while (length > 0);
                sb.Append(suffix);
                parts.Add(sb.ToString());
                continue;
            }
            if (b >= 64)
            {
                // extended dns label RFC 2671
                throw new NotSupportedException("Unsupported extended dns label");
            }
            parts.Add(reader.ReadString(b));
        }
    }

    /// <summary>Tries to parse a domain name.</summary>
    /// <param name="value">The value.</param>
    /// <param name="name">The name.</param>
    /// <returns>Returns true if the value was parsed correctly, false otherwise.</returns>
    public static bool TryParse(string value, out DomainName? name)
    {
        if (value == ".")
        {
            name = Root;
            return true;
        }

        var parts = new List<string>();
        var start = 0;
        string part;
        for (var i = 0; i < value.Length; ++i)
        {
            if ((value[i] == '.') && ((i == 0) || (value[i - 1] != '\\')))
            {
                if (TryParsePart(value[start..i], out part) && (part.Length <= 64))
                {
                    parts.Add(part);
                    start = i + 1;
                }
                else
                {
                    name = null;
                    return false;
                }
            }
        }

        if (value.Length == start)
        {
            // empty label --> name ends with dot
        }
        else if (TryParsePart(value[start..], out part) && (part.Length <= 64))
        {
            parts.Add(part);
        }
        else
        {
            name = null;
            return false;
        }

        name = new([.. parts]);
        return true;
    }

    /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is DomainName other && (ToString().ToLowerInvariant() == other.ToString().ToLowerInvariant());

    /// <summary>Returns a hash code for this instance.</summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode()
    {
        var hash = Parts.Length.GetHashCode();
        foreach (var p in Parts)
        {
            hash ^= p.GetHashCode();
        }

        return hash;
    }

    /// <summary>Gets the parent zone of the domain name.</summary>
    /// <returns>The DomainName of the parent zone.</returns>
    /// <exception cref="Exception">No parent available.</exception>
    public DomainName GetParent()
    {
        if (Parts.Length == 0)
        {
            throw new("No parent available.");
        }

        var parts = new string[Parts.Length - 1];
        Array.Copy(Parts, 1, parts, 0, parts.Length);
        return new(parts);
    }

    /// <summary>Randomizes the character casing.</summary>
    /// <returns>Returns a new <see cref="DomainName"/> instance with random case.</returns>
    public DomainName RandomCase()
    {
        var parts = Parts;
        for (var i = 0; i < parts.Length; i++)
        {
            parts[i] = parts[i].RandomCase();
        }
        return new(parts);
    }

    /// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
    /// <returns>A <see cref="string"/> that represents this instance.</returns>
    public override string ToString() => string.Join(".", Parts);

    #endregion Public Methods
}
