using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Cave.IO;
using Cave.Net;

namespace Cave.Mail.Imap;

/// <summary>Provides a simple imap client.</summary>
public sealed class ImapClient : IDisposable
{
    #region Private Fields

    TcpClient? client;
    int counter = 1;
    Stream? stream;

    #endregion Private Fields

    #region Private Methods

    /// <summary>Releases the unmanaged resources used by this instance and optionally releases the managed resources.</summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (client is IDisposable dispo)
            {
                dispo.Dispose();
            }

            client = null;
            stream?.Dispose();
            stream = null;
        }
    }

    void Login(string? user, string? password)
    {
        if (user is null) throw new ArgumentNullException(nameof(user));
        if (password is null) throw new ArgumentNullException(nameof(password));

        if (user.HasInvalidChars(ASCII.Strings.SafeUrlOptions))
        {
            throw new ArgumentException("User has invalid characters!", nameof(user));
        }

        if (password.HasInvalidChars(ASCII.Strings.SafeUrlOptions))
        {
            throw new ArgumentException("Password has invalid characters!", nameof(password));
        }

        ReadAnswer("*", true);
        SendCommand("LOGIN " + user + " " + password);
    }

    string PrepareLiteralDataCommand(string cmd)
    {
        if (stream is null) throw new InvalidOperationException("Not connected!");
        var id = counter++.ToString("X2");
        var command = id + " " + cmd;
        var writer = new DataWriter(stream);
        writer.Write(ASCII.GetBytes(command + ImapNewLine));
        writer.Flush();
        var answer = ReadAnswer("+", false);
        if (!answer.Result.ToUpperInvariant().StartsWith("+ READY"))
        {
            answer.Throw();
        }

        return id;
    }

    ImapAnswer ReadAnswer(string id, bool throwEx)
    {
        if (stream is null) throw new InvalidOperationException("Not connected!");
        var answer = ImapParser.Parse(id, stream);
        if (throwEx && !answer.Success)
        {
            answer.Throw();
        }

        return answer;
    }

    ImapAnswer SendCommand(string cmd, params object[] parameters) => SendCommand(string.Format(cmd, parameters));

    ImapAnswer SendCommand(string cmd)
    {
        if (stream is null) throw new InvalidOperationException("Not connected!");
        var id = counter++.ToString("X2");
        var command = id + " " + cmd;
        var writer = new DataWriter(stream);
        writer.Write(ASCII.GetBytes(command + ImapNewLine));
        writer.Flush();
        var answer = ReadAnswer(id, true);
        return answer;
    }

    #endregion Private Methods

    #region Public Fields

    /// <summary>The imap new line characters.</summary>
    public const string ImapNewLine = "\r\n";

    #endregion Public Fields

    #region Public Properties

    /// <summary>Gets the name of the log source.</summary>
    /// <value>The name of the log source.</value>
    public string LogSourceName
    {
        get
        {
            if (client != null)
            {
                return "ImapClient <" + client.Client.RemoteEndPoint + ">";
            }

            return "ImapClient <not connected>";
        }
    }

    /// <summary>Gets the selected mailbox.</summary>
    /// <value>The selected mailbox.</value>
    public ImapMailboxInfo? SelectedMailbox { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Closes this instance.</summary>
    public void Close()
    {
        stream?.Close();
        client?.Close();
        Dispose();
    }

    /// <summary>Creates a new mailbox.</summary>
    /// <param name="mailbox">The mailbox.</param>
    public void CreateMailbox(string mailbox) => SendCommand("CREATE \"{0}\"", mailbox);

    /// <summary>Releases all resources used by the this instance.</summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>examines a mailbox.</summary>
    /// <param name="mailboxName">Name of the mailbox.</param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public ImapMailboxInfo Examine(string mailboxName)
    {
        var mailbox = UTF7.Encode(mailboxName);
        var answer = SendCommand("EXAMINE \"" + mailbox + "\"");
        if (!answer.Success)
        {
            throw new(string.Format("Error at examine mailbox {0}: {1}", mailboxName, answer.Result));
        }

        return ImapMailboxInfo.FromAnswer(mailboxName, answer);
    }

    /// <summary>Expunges this instance.</summary>
    /// <exception cref="Exception">Error at store flags.</exception>
    public void Expunge()
    {
        var answer = SendCommand("EXPUNGE");
        if (!answer.Success)
        {
            throw new("Error at store flags");
        }
    }

    /// <summary>Retrieves a message by its internal number (1.. <see cref="ImapMailboxInfo.Exist"/>).</summary>
    /// <param name="number">The internal message number (1.. <see cref="ImapMailboxInfo.Exist"/>).</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    public Rfc822Message GetMessage(int number)
    {
        if (number < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(number));
        }

        var answer = SendCommand("FETCH " + number + " BODY[]");
        var start = Array.IndexOf(answer.Data, (byte)'\n') + 1;
        var end = Array.LastIndexOf(answer.Data, (byte)')') - 2;
        var message = new byte[end - start];
        Array.Copy(answer.Data, start, message, 0, end - start);
        return Rfc822Message.FromBinary(message);
    }

    /// <summary>Retrieves a message by its internal number (1.. <see cref="ImapMailboxInfo.Exist"/>).</summary>
    /// <param name="number">The internal message number (1.. <see cref="ImapMailboxInfo.Exist"/>).</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    public byte[] GetMessageData(int number)
    {
        if (number < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(number));
        }

        var answer = SendCommand("FETCH " + number + " BODY[]");
        var start = Array.IndexOf(answer.Data, (byte)'\n') + 1;
        var end = Array.LastIndexOf(answer.Data, (byte)')');
        var message = new byte[end - start];
        Array.Copy(answer.Data, start, message, 0, end - start);
        return message;
    }

    /// <summary>Retrieves a message header by its internal number (1.. <see cref="ImapMailboxInfo.Exist"/>).</summary>
    /// <param name="number">The internal message number (1.. <see cref="ImapMailboxInfo.Exist"/>).</param>
    /// <returns></returns>
    /// <exception cref="System.ArgumentOutOfRangeException"></exception>
    /// <exception cref="System.FormatException"></exception>
    public Rfc822Message GetMessageHeader(int number)
    {
        if (number < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(number));
        }

        for (int retry = 0; ; retry++)
        {
            if (retry > 3) throw new Exception($"Could not get header of message {number}");
            var answer = SendCommand("FETCH " + number + " BODY[HEADER]");
            if (answer.Data.Length == 0) continue;
            var streamReader = answer.GetStreamReader(0);
            var header = streamReader.ReadLine();
            if (header is null) continue;
            var size = int.Parse(header.Substring(header.LastIndexOf('{')).Unbox("{", "}"));
            var dataReader = answer.GetDataReader(header.Length + 2);
            var messageData = dataReader.ReadBytes(size);
            if (messageData.Length != size) continue;
            var message = Rfc822Message.FromBinary(messageData);
            return message;
        }
    }

    /// <summary>Obtains a list of all present mailboxes.</summary>
    /// <returns></returns>
    public string[] ListMailboxes()
    {
        var list = new List<string>();
        var answer = SendCommand("LIST \"\" *");
        foreach (var line in answer.GetDataLines())
        {
            if (line.StartsWith("* LIST "))
            {
                var parts = ImapParser.SplitAnswer(line);
                var mailboxBytes = ASCII.GetBytes(parts[4].UnboxText(false));
                var mailboxName = UTF7.Decode(mailboxBytes);
                list.Add(mailboxName);
            }
        }
        return [.. list];
    }

    /// <summary>Does a Logon with SSL.</summary>
    /// <param name="con">The connection string.</param>
    public void LoginSSL(ConnectionString con) => LoginSSL(con.UserName, con.Password, con.Server ?? throw new Exception("No server address specified!"), con.GetPort(993));

    /// <summary>Does a Logon with SSL.</summary>
    /// <param name="user">The user.</param>
    /// <param name="password">The password.</param>
    /// <param name="server">The server.</param>
    /// <param name="port">The port.</param>
    public void LoginSSL(string? user, string? password, string server, int port = 993)
    {
        var sslClient = new SslClient();
        sslClient.Connect(server, port);
        sslClient.DoClientTLS(server);
        stream = sslClient.Stream;
        Login(user, password);
    }

    /// <summary>Searches the specified search.</summary>
    /// <param name="search">The search.</param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public ImapNumberSequence Search(ImapSearch search)
    {
        var answer = SendCommand("SEARCH {0}", search);
        if (!answer.Success)
        {
            throw new(string.Format("Error at search {0}", search));
        }

        var sequence = new ImapNumberSequence();
        foreach (var line in answer.GetDataLines())
        {
            if (line.StartsWith("* SEARCH "))
            {
                var s = line.Substring(9);
                if (int.TryParse(s, out var value))
                {
                    sequence += ImapNumberSequence.CreateList(value);
                }
                else
                {
                    sequence += ImapNumberSequence.FromString(s);
                }
            }
        }
        return sequence;
    }

    /// <summary>Selects the mailbox with the specified name.</summary>
    /// <param name="mailboxName">Name of the mailbox.</param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public ImapMailboxInfo Select(string mailboxName)
    {
        var mailbox = UTF7.Encode(mailboxName);
        var answer = SendCommand("SELECT \"{0}\"", mailbox);
        if (!answer.Success)
        {
            throw new(string.Format("Error at select mailbox {0}: {1}", mailboxName, answer.Result));
        }

        SelectedMailbox = ImapMailboxInfo.FromAnswer(mailboxName, answer);
        return SelectedMailbox;
    }

    /// <summary>Sets the specified flags at the message with the given number.</summary>
    /// <param name="number">The number.</param>
    /// <param name="flags">The flags.</param>
    /// <exception cref="Exception">Error at store flags.</exception>
    public void SetFlags(int number, params string[] flags)
    {
        var answer = SendCommand("STORE {0} +FLAGS ({1})", number, string.Join(" ", flags));
        if (!answer.Success)
        {
            throw new("Error at store flags");
        }
    }

    /// <summary>Uploads a message to the specified mailbox.</summary>
    /// <param name="mailboxName">Name of the mailbox.</param>
    /// <param name="messageData">The message data.</param>
    /// <exception cref="System.ArgumentNullException">messageData.</exception>
    public void UploadMessageData(string mailboxName, byte[] messageData)
    {
        if (stream is null) throw new InvalidOperationException("Not connected!");
        var mailbox = UTF7.Encode(mailboxName);
        if (messageData == null)
        {
            throw new ArgumentNullException(nameof(messageData));
        }

        var id = PrepareLiteralDataCommand("APPEND \"" + mailbox + "\" (\\Seen) {" + messageData.Length + "}");
        var writer = new DataWriter(stream);
        writer.Write(messageData);
        writer.Write((byte)13);
        writer.Write((byte)10);
        writer.Flush();
        ReadAnswer(id, true);
    }

    #endregion Public Methods
}
