using System.Diagnostics.CodeAnalysis;

namespace Cave.Mail.Imap;

/// <summary>Provides available search types for imap searches.</summary>
[SuppressMessage("Naming", "CA1707", Justification = "Underlines needed in names for compatibility.")]
public enum ImapSearchType
{
    /// <summary>Multiple other searches combined by AND or OR</summary>
    _MULTIPLE,

    /// <summary>All messages in the mailbox</summary>
    ALL,

    /// <summary>Messages with the \Answered flag set.</summary>
    ANSWERED,

    /// <summary>Parameter: string - Messages that contain the specified string in the envelope structure's BCC field.</summary>
    BCC,

    /// <summary>Parameter: date - Messages whose internal date (disregarding time and timezone) is earlier than the specified date.</summary>
    BEFORE,

    /// <summary>Parameter: string - Messages that contain the specified string in the body of the message.</summary>
    BODY,

    /// <summary>Parameter: string - Messages that contain the specified string in the envelope structure's CC field.</summary>
    CC,

    /// <summary>Messages with the \Deleted flag set.</summary>
    DELETED,

    /// <summary>Messages with the \Draft flag set.</summary>
    DRAFT,

    /// <summary>Messages with the \Flagged flag set.</summary>
    FLAGGED,

    /// <summary>Parameter: string - Messages that contain the specified string in the envelope structure's FROM field.</summary>
    FROM,

    /// <summary>
    /// Parameter: field-name string - Messages that have a header with the specified field-name (as defined in [RFC-2822]) and that
    /// contains the specified string in the text of the header (what comes after the colon). If the string to search is zero-length, this matches
    /// all messages that have a header line with the specified field-name regardless of the contents.
    /// </summary>
    HEADER,

    /// <summary>Parameter: flag - Messages with the specified keyword flag set.</summary>
    KEYWORD,

    /// <summary>Parameter: n - Messages with an [RFC-2822] size larger than the specified number of octets.</summary>
    LARGER,

    /// <summary>Messages that have the \Recent flag set but not the \Seen flag. This is functionally equivalent to "(RECENT UNSEEN)".</summary>
    NEW,

    /// <summary>Parameter: search-key - Messages that do not match the specified search key.</summary>
    NOT,

    /// <summary>Messages that do not have the \Recent flag set. This is functionally equivalent to "NOT RECENT" (as opposed to "NOT NEW").</summary>
    OLD,

    /// <summary>Parameter: date - Messages whose internal date (disregarding time and timezone) is within the specified date.</summary>
    ON,

    /// <summary>Parameter: search-key1 search-key2 - Messages that match either search key.</summary>
    OR,

    /// <summary>Messages that have the \Recent flag set.</summary>
    RECENT,

    /// <summary>Messages that have the \Seen flag set.</summary>
    SEEN,

    /// <summary>Parameter: date - Messages whose [RFC-2822] Date: header (disregarding time and timezone) is earlier than the specified date.</summary>
    SENTBEFORE,

    /// <summary>Parameter: date - Messages whose [RFC-2822] Date: header (disregarding time and timezone) is within the specified date.</summary>
    SENTON,

    /// <summary>
    /// Parameter: date - Messages whose [RFC-2822] Date: header (disregarding time and timezone) is within or later than the specified
    /// date.
    /// </summary>
    SENTSINCE,

    /// <summary>Parameter: date - Messages whose internal date (disregarding time and timezone) is within or later than the specified date.</summary>
    SINCE,

    /// <summary>Parameter: n - Messages with an [RFC-2822] size smaller than the specified number of octets.</summary>
    SMALLER,

    /// <summary>Parameter: string - Messages that contain the specified string in the envelope structure's SUBJECT field.</summary>
    SUBJECT,

    /// <summary>Parameter: string - Messages that contain the specified string in the header or body of the message.</summary>
    TEXT,

    /// <summary>Parameter: string - Messages that contain the specified string in the envelope structure's TO field.</summary>
    TO,

    /// <summary>
    /// Parameter: sequence set - Messages with unique identifiers corresponding to the specified unique identifier set. Sequence set
    /// ranges are permitted.
    /// </summary>
    UID,

    /// <summary>Messages that do not have the \Answered flag set.</summary>
    UNANSWERED,

    /// <summary>Messages that do not have the \Deleted flag set.</summary>
    UNDELETED,

    /// <summary>Messages that do not have the \Draft flag set.</summary>
    UNDRAFT,

    /// <summary>Messages that do not have the \Flagged flag set.</summary>
    UNFLAGGED,

    /// <summary>Parameter: flag - Messages that do not have the specified keyword flag set.</summary>
    UNKEYWORD,

    /// <summary>Messages that do not have the \Seen flag set.</summary>
    UNSEEN
}
