using System;
using System.Net.Mail;
using Cave.Collections.Generic;
using Cave.Net;

namespace Cave.Mail;

/// <summary>Provides an <see cref="MailAddress" /> Extension for verifying an email address at the mail server responsible for the address.</summary>
public static class MailAddressExtension
{
    /// <summary>Checks the specified email address for validity with the mail server responsible for the address.</summary>
    /// <param name="address">The email address to verify.</param>
    public static void Verify(this MailAddress address)
    {
        var validator = new SmtpValidator(NetTools.HostName, new(Environment.UserName + '@' + NetTools.HostName));
        validator.Validate(address, true);
    }

    /// <summary>Checks the specified email address for validity with the mail server responsible for the address.</summary>
    /// <param name="address">The email address to verify.</param>
    /// <param name="serverName">Name of the server.</param>
    /// <exception cref="ArgumentOutOfRangeException">serverName;ServerName needs to be a full qualified domain name!.</exception>
    public static void Verify(this MailAddress address, string serverName)
    {
        var i = serverName.IndexOf('.');
        var n = i == -1 ? -1 : serverName.IndexOf('.', i + 1);
        if (n < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(serverName), "ServerName needs to be a full qualified domain name!");
        }

        var email = serverName[..i] + '@' + serverName[(i + 1)..];
        var validator = new SmtpValidator(serverName, new(email));
        validator.Validate(address, true);
    }

    /// <summary>Loads the addresses from a address array. Each address is checked for validity and uniqueness.</summary>
    /// <param name="receipients">The receipients.</param>
    /// <param name="addresses">The addresses.</param>
    /// <param name="throwErrors">if set to <c>true</c> [throw errors].</param>
    public static void LoadAddresses(this Set<MailAddress> receipients, string[] addresses, bool throwErrors = false)
    {
        foreach (var address in addresses)
        {
            try
            {
                receipients.Include(new(address));
            }
            catch
            {
                if (throwErrors)
                {
                    throw;
                }
            }
        }
    }
}
