using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using Cave.IO;

namespace Cave.Net.Dns;

/// <summary>Provides a dns record.</summary>
public class DnsRecord
{
    #region Public Properties

    /// <summary>Gets the domain name.</summary>
    /// <value>The name.</value>
    public DomainName Name { get; private set; }

    /// <summary>Gets the record class.</summary>
    /// <value>The record class.</value>
    public DnsRecordClass RecordClass { get; private set; }

    /// <summary>Gets the type of the record.</summary>
    /// <value>The type of the record.</value>
    public DnsRecordType RecordType { get; private set; }

    /// <summary>Gets the time to live.</summary>
    /// <value>The time to live.</value>
    public int TimeToLive { get; private set; }

    /// <summary>Gets the value.</summary>
    /// <value>The value.</value>
    public object Value { get; private set; }

    #endregion Public Properties

    #region Public Methods

    /// <summary>Parses a record from the specified reader.</summary>
    /// <param name="reader">The reader.</param>
    /// <returns>Returns the dns record found.</returns>
    /// <exception cref="NotImplementedException">RecordType not implemented.</exception>
    public static DnsRecord Parse(DataReader reader)
    {
        var result = new DnsRecord
        {
            Name = DomainName.Parse(reader),
            RecordType = (DnsRecordType)reader.ReadUInt16(),
            RecordClass = (DnsRecordClass)reader.ReadUInt16(),
            TimeToLive = reader.ReadInt32()
        };

        int length = reader.ReadUInt16();
        if (length > reader.Available)
        {
            Trace.WriteLine("Additional data after dns answer.");
        }
        try
        {
            result.Value = result.RecordType switch
            {
                DnsRecordType.A or DnsRecordType.AAAA => new IPAddress(reader.ReadBytes(length)),
                DnsRecordType.SOA => SoaRecord.Parse(reader),
                DnsRecordType.MX => MxRecord.Parse(reader),
                DnsRecordType.TXT => TxtRecord.Parse(reader, length),
                _ => DomainName.Parse(reader)
            };
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"RecordType {result.RecordType} cannot be read using DomainName.Parse()!", ex);
        }
        return result;
    }

    /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
    /// <returns>A <see cref="string" /> that represents this instance.</returns>
    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append(Name);
        result.Append(' ');
        result.Append(RecordClass);
        result.Append(' ');
        result.Append(RecordType);
        if (Value != null)
        {
            result.Append(' ');
            result.Append(Value);
        }
        return result.ToString();
    }

    #endregion Public Methods
}
