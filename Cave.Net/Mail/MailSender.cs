using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using Cave.Collections.Generic;
using Cave.IO;

namespace Cave.Mail;

/// <summary>Provides mail sending.</summary>
public class MailSender : IDisposable
{
    /// <summary>Loads some addresses into the target collection.</summary>
    /// <param name="target">Target collection</param>
    /// <param name="addressText">Semicolon separated text containing valid email addresses</param>
    public static void LoadAddresses(ICollection<MailAddress> target, string addressText)
    {
        if (addressText != null)
        {
            foreach (var address in addressText.Split(';'))
            {
                if (address.Trim() == "")
                {
                    continue;
                }

                target.Add(new(address));
            }
        }
    }

    SmtpClient client;

    /// <summary>Initializes a new instance of the <see cref="MailSender" /> class.</summary>
    public MailSender() => client = new();

    /// <summary>The sender address.</summary>
    public MailAddress Sender { get; set; }

    /// <summary>Gets the blind carbon copy addresses added to all sent messages.</summary>
    /// <value>The blind carbon copy addresses added to all sent messages.</value>
    public Set<MailAddress> BCC { get; } = new();

    /// <summary>Gets the carbon copy addresses added to all sent messages.</summary>
    /// <value>The carbon copy addresses added to all sent messages.</value>
    public Set<MailAddress> CC { get; } = new();

    /// <summary>Enable or disable ssl.</summary>
    public bool EnableSsl => client.EnableSsl;

    /// <summary>The mail server to use.</summary>
    public string Server => client.Host;

    /// <inheritdoc />
    public void Dispose()
    {
        if (client is IDisposable disposable)
        {
            disposable.Dispose();
        }
        client = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>Loads the config for the sender from an ini file. This reads [Mail] Username, Password, Server, Sender, DisableSSL, BCC and CC.</summary>
    /// <param name="settings">The settings.</param>
    public virtual void LoadConfig(IniReader settings)
    {
        try
        {
            var user = settings.ReadSetting("Mail", "Username");
            var pass = settings.ReadSetting("Mail", "Password");
            var server = settings.ReadString("Mail", "Server", "localhost");
            var sender = settings.ReadString("Mail", "Sender", "postmaster@" + server);
            var bccString = settings.ReadSetting("Mail", "BCC");
            LoadAddresses(BCC, bccString);
            var ccString = settings.ReadSetting("Mail", "CC");
            LoadAddresses(CC, bccString);
            Sender = new(sender);

            client.EnableSsl = settings.ReadBool("Mail", "DisableSSL", false) != true;
            client.Host = server;
            if ((user != null) && (pass != null))
            {
                client.Credentials = new NetworkCredential(user, pass);
            }
        }
        catch (Exception ex)
        {
            var msg = string.Format("Error while loading configuration from {0}", settings);
            Trace.TraceError(msg);
            throw new(msg, ex);
        }
    }

    /// <summary>Sets the credentials.</summary>
    /// <param name="credentials"></param>
    public void SetCredentials(NetworkCredential credentials) => client.Credentials = credentials;

    /// <summary>Sends the specified message.</summary>
    /// <param name="message">The message.</param>
    /// <param name="retries">The retries.</param>
    /// <param name="throwException">if set to <c>true</c> [throw exception].</param>
    /// <returns></returns>
    public virtual bool Send(MailMessage message, int retries = 3, bool throwException = true)
    {
        foreach (var bcc in BCC)
        {
            message.Bcc.Add(bcc);
        }
        foreach (var cc in CC)
        {
            message.CC.Add(cc);
        }

        message.From = Sender;
        for (var i = 1;; i++)
        {
            try
            {
                client.Timeout = 5000;
                Trace.TraceInformation("Sending mail message <cyan>{0}<default> to <cyan>{1}", message.Subject, message.To);
                client.Send(message);
                Trace.TraceInformation("Sent mail message <cyan>{0}<default> to <cyan>{1}", message.Subject, message.To);
                return true;
            }
            catch
            {
                if (i > retries)
                {
                    if (throwException)
                    {
                        Trace.TraceError("Error while sending mail to <red>{0}", message.To);
                        throw;
                    }

                    return false;
                }
                Trace.TraceError("Error while sending mail to <red>{0}<default>. Try <red>{1}<default>..", message.To, i);
            }
        }
    }
}
