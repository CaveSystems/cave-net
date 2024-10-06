using System;
using System.IO;
using System.Text;
using Cave.IO;

namespace Cave.Mail.Imap;

struct ImapAnswer
{
    #region Internal Fields

    internal static readonly string[] ImapNewLineSeparator = ["\r\n"];

    #endregion Internal Fields

    #region Internal Methods

    internal void Throw()
    {
        var index = Result.IndexOf(' ', ID.Length + 1) + 1;
        if (index <= 0)
        {
            throw new InvalidOperationException();
        }
        throw new InvalidOperationException(Result[index..]);
    }

    #endregion Internal Methods

    #region Public Fields

    public byte[] Data;
    public string ID;

    public string Result;

    #endregion Public Fields

    #region Public Properties

    public bool Success => Result.StartsWith(ID + " OK");

    #endregion Public Properties

    #region Public Methods

    public string[] GetDataLines() => GetDataString().Split(ImapNewLineSeparator, StringSplitOptions.None);

    /// <summary>Obtains a DataReader for the current answer.</summary>
    /// <param name="start"></param>
    /// <returns></returns>
    public DataReader GetDataReader(long start)
    {
        if (start == 0)
        {
            return new(new MemoryStream(Data), StringEncoding.US_ASCII);
        }
        return new(new SubStream(new MemoryStream(Data), (int)start), StringEncoding.US_ASCII);
    }

    public string GetDataString() => ImapConst.ISO88591.GetString(Data);

    /// <summary>Obtains a StreamReader for the current answer.</summary>
    /// <param name="start"></param>
    /// <returns></returns>
    public StreamReader GetStreamReader(long start)
    {
        if (start == 0)
        {
            return new(new MemoryStream(Data), Encoding.ASCII);
        }
        return new(new SubStream(new MemoryStream(Data), (int)start), Encoding.ASCII);
    }

    /// <summary>Obtains the result.</summary>
    /// <returns></returns>
    public override string ToString() => Result;

    #endregion Public Methods
}
