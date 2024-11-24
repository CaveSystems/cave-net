using System.Linq;
using System.Net;
using Cave.Net.Dns;

namespace SubnetScan;

internal sealed class Info
{
    #region Public Constructors

    public Info(IPAddress address)
    {
        Address = address;
        try
        {
            var response = DnsClient.Default.ResolveSequential(Address);
            Hostname = $"{response?.Answers.First().Value}";
        }
        catch { }
    }

    #endregion Public Constructors

    #region Public Properties

    public IPAddress Address { get; }

    public string Hostname { get; private set; } = string.Empty;

    #endregion Public Properties

    #region Public Methods

    public override string ToString() => $"{Address} {Hostname}";

    #endregion Public Methods
}
