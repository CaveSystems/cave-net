using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Sockets;
using Cave.Net;
using Cave.Net.Dns;

namespace Cave.Mail;

/// <summary>Provides email validation by asking the reciepients smtp server.</summary>
public class SmtpValidator
{
    #region Private Fields

    static readonly char[] AnswerSeparator = [' '];

    #endregion Private Fields

    #region Private Methods

    static int ParseAnswer(string? answer)
    {
        if (answer is null) throw new InvalidDataException("SmtpValidator_ProtocolError");
        var parts = answer.Split(AnswerSeparator, 2);
        if (!int.TryParse(parts[0], out var code))
        {
            throw new InvalidDataException("SmtpValidator_ProtocolError");
        }
        if (parts.Length > 1)
        {
            Trace.TraceInformation("Server_AnswerWithResult {0} {1}", code, parts[1]);
        }
        else
        {
            Trace.TraceInformation("Server_Answer {0}", code);
        }
        return code;
    }

    static bool ResultIsValid(int v) => v is >= 250 and < 260;

    #endregion Private Methods

    #region Public Constructors

    /// <summary>Initializes a new instance of the <see cref="SmtpValidator"/> class.</summary>
    /// <param name="server">Our full qualified server address.</param>
    /// <param name="sender">Our email address.</param>
    public SmtpValidator(string server, MailAddress sender)
    {
        if (!DnsClient.Default.GetHostAddresses(server).Any())
        {
            throw new NetworkException("Cannot find my own server name in dns!");
        }

        Server = server;
        Sender = sender;
    }

    #endregion Public Constructors

    #region Public Enums

    /// <summary>Provides available <see cref="SmtpValidator"/> results.</summary>
    public enum SmtpValidatorResult
    {
        /// <summary>success</summary>
        Success = 0,

        /// <summary>error: network not available</summary>
        ErrorMyNetwork = 1,

        /// <summary>error: my settings are invalid (target does not accept <see cref="SmtpValidator"/> as sender)</summary>
        ErrorMySettings,

        /// <summary>error: connection to the server cannot be established</summary>
        ErrorServer = 0x101,

        /// <summary>error address is invalid</summary>
        ErrorAddress
    }

    #endregion Public Enums

    #region Public Properties

    /// <summary>Gets our email address (has to exist).</summary>
    /// <value>Our email address.</value>
    public MailAddress Sender { get; }

    /// <summary>Gets our full qualified server address (has to match rdns).</summary>
    /// <value>Our full qualified server address.</value>
    public string Server { get; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Validates the specified target email address.</summary>
    /// <param name="target">The target email address.</param>
    /// <param name="throwException">if set to <c>true</c> [throw exception].</param>
    /// <returns>bool on success, false otherwise.</returns>
    /// <exception cref="ArgumentException">
    /// Server not available!;target.Host or Server does not accept me as sender!;Server or Target address does not exist!;target.Address or Server does not
    /// accept me as sender!;Sender.Address or Target address does not exist!;target.Address.
    /// </exception>
    /// <exception cref="InvalidDataException">Smtp protocol error!.</exception>
    public SmtpValidatorResult Validate(MailAddress target, bool throwException)
    {
        foreach (var port in new[] { 25, 587 })
        {
            try
            {
                using var client = new TcpClient(target.Host, port);
                using Stream stream = client.GetStream();
                var writer = new StreamWriter(stream);
                var reader = new StreamReader(stream);
                if (ParseAnswer(reader.ReadLine()) != 220)
                {
                    if (throwException)
                    {
                        throw new ArgumentException($"SmtpValidator_ServerNotAvailable {target}");
                    }

                    return SmtpValidatorResult.ErrorServer;
                }

                writer.WriteLine("HELO " + Server);
                if (!ResultIsValid(ParseAnswer(reader.ReadLine())))
                {
                    if (throwException)
                    {
                        throw new ArgumentException($"SmtpValidator_ServerDoesNotAcceptMe {target}");
                    }

                    return SmtpValidatorResult.ErrorMySettings;
                }

                writer.WriteLine("VRFY " + target.Address);
                if (!ResultIsValid(ParseAnswer(reader.ReadLine())))
                {
                    if (throwException)
                    {
                        throw new ArgumentException($"Error_TargetAddressInvalid {target}");
                    }

                    return SmtpValidatorResult.ErrorAddress;
                }

                writer.WriteLine("MAIL " + Sender.Address);
                if (!ResultIsValid(ParseAnswer(reader.ReadLine())))
                {
                    if (throwException)
                    {
                        throw new ArgumentException($"SmtpValidator_ServerDoesNotAcceptMe {target}");
                    }

                    return SmtpValidatorResult.ErrorMySettings;
                }

                writer.WriteLine("RCPT " + target.Address);
                if (!ResultIsValid(ParseAnswer(reader.ReadLine())))
                {
                    if (throwException)
                    {
                        throw new ArgumentException($"Error_TargetAddressInvalid {target}");
                    }

                    return SmtpValidatorResult.ErrorAddress;
                }

                writer.WriteLine("RSET");
                var s = reader.ReadLine();

                writer.WriteLine("QUIT");
                s = reader.ReadLine();
                return SmtpValidatorResult.Success;
            }
            catch (SocketException ex)
            {
                if (port == 587)
                {
                    if (throwException)
                    {
                        throw;
                    }

                    Trace.TraceWarning($"Socket error <red>{ex.SocketErrorCode}<default>!");
                    return ex.SocketErrorCode switch
                    {
                        SocketError.ConnectionRefused or SocketError.ConnectionReset or SocketError.HostDown or SocketError.HostNotFound or SocketError.HostUnreachable => SmtpValidatorResult.ErrorServer,
                        _ => SmtpValidatorResult.ErrorMyNetwork
                    };
                }
            }
            catch
            {
                if (port == 587)
                {
                    if (throwException)
                    {
                        throw;
                    }
                }
                Trace.TraceError("Error during smtp validation!");
            }
        }

        return SmtpValidatorResult.ErrorServer;
    }

    #endregion Public Methods
}
