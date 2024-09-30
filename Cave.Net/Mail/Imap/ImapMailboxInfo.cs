using System;
using System.Diagnostics.CodeAnalysis;

namespace Cave.Mail.Imap;

/// <summary>Provides information about an imap mailbox.</summary>
public class ImapMailboxInfo : IEquatable<ImapMailboxInfo>
{
    #region Private Fields

    static readonly char[] SpaceSeparator = [' '];

    #endregion Private Fields

    #region Internal Methods

    internal static ImapMailboxInfo FromAnswer(string name, ImapAnswer answer)
    {
        var result = new ImapMailboxInfo
        {
            Name = name
        };
        foreach (var line in answer.GetDataLines())
        {
            if (line.StartsWith("* "))
            {
                var parts = ImapParser.SplitAnswer(line);
                switch (parts[1])
                {
                    case "FLAGS":
                        result.Flags = parts[2].UnboxBrackets().Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
                        continue;
                    case "OK":
                        if (parts.Length > 2)
                        {
                            var subParts = ImapParser.SplitAnswer(parts[2].UnboxBrackets());
                            switch (subParts[0])
                            {
                                case "PERMANENTFLAGS":
                                    result.PermanentFlags = subParts[1].UnboxBrackets().Split(SpaceSeparator, StringSplitOptions.RemoveEmptyEntries);
                                    continue;
                                case "UIDVALIDITY":
                                    result.UidValidity = uint.Parse(subParts[1]);
                                    continue;
                                case "UIDNEXT":
                                    result.UidNext = uint.Parse(subParts[1]);
                                    continue;
                                case "UNSEEN":
                                    result.Unseen = uint.Parse(subParts[1]);
                                    continue;
                            }
                        }
                        break;

                    default:
                        switch (parts[2])
                        {
                            case "EXISTS":
                                result.Exist = int.Parse(parts[1]);
                                continue;
                            case "RECENT":
                                result.Recent = int.Parse(parts[1]);
                                continue;
                        }
                        break;
                }
            }
        }
        return result;
    }

    #endregion Internal Methods

    #region Public Properties

    /// <summary>Number of messages present.</summary>
    public int Exist { get; private set; }

    /// <summary>The flags.</summary>
    public string[] Flags { get; private set; } = [];

    /// <summary>Name of the Mailbox.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Gets the flags that can be set permanently.</summary>
    /// <value>The permanent flags.</value>
    public string[] PermanentFlags { get; private set; } = [];

    /// <summary>Number of recent messages.</summary>
    public int Recent { get; private set; }

    /// <summary>Gets the uid next.</summary>
    /// <value>The uid next.</value>
    public uint UidNext { get; private set; }

    /// <summary>Gets the uid validity value. A change on this indicates invalidation of all cached uids.</summary>
    /// <value>The uid validity.</value>
    public uint UidValidity { get; private set; }

    /// <summary>Gets the first unseen message index (0 == none).</summary>
    /// <value>The unseen.</value>
    public uint Unseen { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Implements the operator !=.</summary>
    /// <param name="value1">The value1.</param>
    /// <param name="value2">The value2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator !=(ImapMailboxInfo value1, ImapMailboxInfo value2)
    {
        if (value1 is null)
        {
            return value2 is not null;
        }

        if (value2 is null)
        {
            return true;
        }

        return !value1.Equals(value2);
    }

    /// <summary>Implements the operator ==.</summary>
    /// <param name="value1">The value1.</param>
    /// <param name="value2">The value2.</param>
    /// <returns>The result of the operator.</returns>
    public static bool operator ==(ImapMailboxInfo value1, ImapMailboxInfo value2)
    {
        if (value1 is null)
        {
            return value2 is null;
        }

        if (value2 is null)
        {
            return false;
        }

        return value1.Equals(value2);
    }

    /// <summary>Determines whether the specified <see cref="ImapMailboxInfo"/>, is equal to this instance.</summary>
    /// <param name="other">The <see cref="ImapMailboxInfo"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="ImapMailboxInfo"/> is equal to this instance; otherwise, <c>false</c>.</returns>
    public bool Equals(ImapMailboxInfo? other) => other is not null && (other.Exist == Exist) && (other.Name == Name) && (other.Recent == Recent);

    /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
    /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
    public override bool Equals(object? obj) => obj is ImapMailboxInfo info && base.Equals(info);

    /// <summary>Returns a hash code for this instance.</summary>
    /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
    public override int GetHashCode() => base.GetHashCode();

    /// <summary>Provides a string "Name [Recent] [Exist]".</summary>
    /// <returns>A <see cref="string"/> that represents this instance.</returns>
    public override string ToString() =>
        Name +
        (Recent > 0 ? "[" + Recent + " Recent]" : "") +
        (Exist > 0 ? "[" + Exist + " Exist]" : "");

    #endregion Public Methods
}
