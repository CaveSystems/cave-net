using Cave.IO;

namespace Cave.Net.Dns;

/// <summary>Provides a MaileXchange dns record.</summary>
public struct MxRecord
{
    #region Public Fields

    /// <summary>The exchange domain name.</summary>
    public DomainName ExchangeDomainName;

    /// <summary>The preference value.</summary>
    public ushort Preference;

    #endregion Public Fields

    #region Public Methods

    /// <summary>Parses a record using the specified reader.</summary>
    /// <param name="reader">The reader.</param>
    /// <returns>Returns a new <see cref="MxRecord" /> structure.</returns>
    public static MxRecord Parse(DataReader reader)
    {
        var result = new MxRecord
        {
            Preference = reader.ReadUInt16(),
            ExchangeDomainName = DomainName.Parse(reader)
        };
        return result;
    }

    /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
    /// <returns>A <see cref="string" /> that represents this instance.</returns>
    public override string ToString() => string.Format("{0}, {1}", Preference, ExchangeDomainName);

    #endregion Public Methods
}
