using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Threading;
using Cave.Collections.Generic;
using Cave.IO;

namespace Cave.Mail;

/// <summary>Provides (html) email sending.</summary>
public class SimpleMailer
{
    #region Private Fields

    string? password;
    int port;
    string? server;
    string? username;

    #endregion Private Fields

    #region Public Properties

    /// <summary>Gets the BCC addresses.</summary>
    /// <value>The BCC addresses.</value>
    public Set<MailAddress> Bcc { get; } = new();

    /// <summary>Gets or sets the content HTML.</summary>
    /// <value>The content HTML.</value>
    public string ContentHtml { get; set; } = string.Empty;

    /// <summary>Gets or sets the content text.</summary>
    /// <value>The content text.</value>
    public string ContentText { get; set; } = string.Empty;

    /// <summary>Gets or sets from address.</summary>
    /// <value>From address.</value>
    public MailAddress? From { get; set; }

    /// <summary>Gets the name of the log source.</summary>
    /// <value>The name of the log source.</value>
    public string LogSourceName
    {
        get
        {
            if (Username.Contains('@'))
            {
                return $"SimpleMailer {Username}";
            }

            return $"SimpleMailer {Username}@{Server}";
        }
    }

    /// <summary>Gets or sets the password.</summary>
    /// <value>The password.</value>
    public string Password { get => password ?? string.Empty; set => password = value; }

    /// <summary>Gets or sets the port.</summary>
    /// <value>The port.</value>
    public int Port { get => port; set => port = value; }

    /// <summary>Gets or sets the server.</summary>
    /// <value>The server.</value>
    public string Server { get => server ?? string.Empty; set => server = value; }

    /// <summary>Gets or sets the subject.</summary>
    /// <value>The subject.</value>
    public string Subject { get; set; } = string.Empty;

    /// <summary>Gets to addresses.</summary>
    /// <value>Toaddresses.</value>
    public Set<MailAddress> To { get; } = new();

    /// <summary>Gets or sets the username.</summary>
    /// <value>The username.</value>
    public string Username { get => username ?? string.Empty; set => username = value; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Loads the mailer configuration from the specified ini file. This reads [Mail] Server, Port, Password and Username.</summary>
    /// <param name="config">The configuration.</param>
    public void LoadConfig(IniReader config)
    {
        if (!config.GetValue("Mail", "Server", ref server) ||
            !config.GetValue("Mail", "Port", ref port) ||
            !config.GetValue("Mail", "Password", ref password) ||
            !config.GetValue("Mail", "Username", ref username))
        {
            throw new("[Mail] configuration is invalid!");
        }
        //TODO: Optional Display Name for Sender
        var from = config.ReadSetting("Mail", "From");
        From = from is null ? null : new(from);
        To.LoadAddresses(config.ReadSection("SendTo", true));
        Bcc.LoadAddresses(config.ReadSection("BlindCarbonCopy", true));
    }

    /// <summary>Loads a content from html and txt file for the specified culture.</summary>
    /// <param name="folder">The folder.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="culture">The culture.</param>
    public void LoadContent(string folder, string fileName, CultureInfo culture)
    {
        var path = Path.Combine(folder, fileName + "." + culture.TwoLetterISOLanguageName + ".html");
        if (File.Exists(path))
        {
            ContentHtml = File.ReadAllText(path);
        }
        else
        {
            ContentHtml = File.ReadAllText(Path.Combine(folder, fileName + ".html"));
        }
        path = Path.Combine(folder, fileName + "." + culture.TwoLetterISOLanguageName + ".txt");
        if (File.Exists(path))
        {
            ContentHtml = File.ReadAllText(path);
        }
        else
        {
            ContentHtml = File.ReadAllText(Path.Combine(folder, fileName + ".txt"));
        }
    }

    /// <summary>Sends an email.</summary>
    public void Send(Dictionary<string, string>? headers = null)
    {
        if (To.Count == 0)
        {
            throw new("No recepient (SendTo) address.");
        }

        using var message = new MailMessage();
        if (headers != null)
        {
            foreach (var i in headers)
            {
                message.Headers[i.Key] = i.Value;
            }
        }
        foreach (var a in To)
        {
            message.To.Add(a);
        }

        foreach (var a in Bcc)
        {
            message.Bcc.Add(a);
        }

        message.Subject = Subject;
        if (From is not null) message.From = From;
        var plainText = AlternateView.CreateAlternateViewFromString(ContentText, null, MediaTypeNames.Text.Plain);
        var htmlText = AlternateView.CreateAlternateViewFromString(ContentHtml, Encoding.UTF8, MediaTypeNames.Text.Html);
        message.AlternateViews.Add(plainText);
        message.AlternateViews.Add(htmlText);
        for (var i = 0; ; i++)
        {
            try
            {
                var client = new SmtpClient(Server, Port)
                {
                    Timeout = 50000,
                    EnableSsl = true,
                    Credentials = new NetworkCredential(Username, Password)
                };
                client.Send(message);
                (client as IDisposable)?.Dispose();
                Trace.TraceInformation("Sent email '<green>{0}<default>' to <cyan>{1}", message.Subject, message.To);
                break;
            }
            catch
            {
                if (i > 3)
                {
                    throw;
                }

                Thread.Sleep(1000);
            }
        }
    }

    #endregion Public Methods
}
