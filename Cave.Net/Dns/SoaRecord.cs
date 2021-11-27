using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>Provides a soa record.</summary>
    public struct SoaRecord
    {
        #region Private Methods

        private static MailAddress ParseEmailAddress(DataReader reader)
        {
            long endposition = -1;
            var parts = new List<string>();
            while (true)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    // end of domain, RFC1035
                    if (endposition > 0)
                    {
                        reader.BaseStream.Position = endposition;
                    }

                    return (parts.Count == 0)
                        ? null
                        : parts.Any(p => p.Contains("@"))
                        ? new MailAddress(parts.Join("."))
                        : new MailAddress(parts[0] + "@" + parts.SubRange(1).Join("."));
                }
                if (b >= 192)
                {
                    // Pointer, RFC1035
                    var pointer = ((b - 192) * 256) + reader.ReadByte();

                    // save position
                    if (endposition < 0)
                    {
                        endposition = reader.BaseStream.Position;
                    }

                    reader.BaseStream.Position = pointer;
                    continue;
                }
                if (b == 65)
                {
                    // binary EDNS label, RFC2673, RFC3363, RFC3364
                    int length = reader.ReadByte();
                    if (length == 0)
                    {
                        length = 256;
                    }

                    var sb = new StringBuilder();
                    sb.Append(@"\[x");
                    var suffix = "/" + length + "]";
                    do
                    {
                        b = reader.ReadByte();
                        if (length < 8)
                        {
                            b &= (byte)(0xff >> (8 - length));
                        }
                        sb.Append(b.ToString("x2"));
                        length -= 8;
                    }
                    while (length > 0);
                    sb.Append(suffix);
                    parts.Add(sb.ToString());
                    continue;
                }
                if (b >= 64)
                {
                    // extended dns label RFC 2671
                    throw new NotSupportedException("Unsupported extended dns label");
                }
                parts.Add(reader.ReadString(b));
            }
        }

        #endregion Private Methods

        #region Public Fields

        /// <summary>The expire interval (seconds).</summary>
        public int ExpireInterval;

        /// <summary>The master name.</summary>
        public DomainName MasterName;

        /// <summary>The negative caching TTL (seconds).</summary>
        public int NegativeCachingTTL;

        /// <summary>The refresh interval (seconds).</summary>
        public int RefreshInterval;

        /// <summary>The responsible name.</summary>
        public MailAddress ResponsibleName;

        /// <summary>The retry interval (seconds).</summary>
        public int RetryInterval;

        /// <summary>The serial number.</summary>
        public uint SerialNumber;

        #endregion Public Fields

        #region Public Methods

        /// <summary>Parses the record using the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <returns>Returns a new <see cref="SoaRecord"/> structure.</returns>
        public static SoaRecord Parse(DataReader reader)
        {
            DomainName masterName;
            MailAddress responsibleName;
            try
            {
                masterName = DomainName.Parse(reader);
                responsibleName = ParseEmailAddress(reader);
            }
            catch (Exception ex)
            {
                masterName = null;
                responsibleName = null;
                Trace.TraceError(ex.ToString());
            }

            var result = new SoaRecord
            {
                MasterName = masterName,
                ResponsibleName = responsibleName,
                SerialNumber = reader.ReadUInt32(),
                RefreshInterval = reader.ReadInt32(),
                RetryInterval = reader.ReadInt32(),
                ExpireInterval = reader.ReadInt32(),
                NegativeCachingTTL = reader.ReadInt32(),
            };
            return result;
        }

        /// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
        public override string ToString() =>
            string.Format(
                "{0}. {1}. (\n" +
                "\t{2} ; serial\n" +
                "\t{3} ; refresh\n" +
                "\t{4} ; retry\n" +
                "\t{5} ; expire\n" +
                "\t{6} ) ; ttl",
                MasterName,
                ResponsibleName,
                SerialNumber,
                RefreshInterval,
                RetryInterval,
                ExpireInterval,
                NegativeCachingTTL);

        #endregion Public Methods
    }
}
