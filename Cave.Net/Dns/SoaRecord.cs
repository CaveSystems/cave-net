using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Cave.IO;

namespace Cave.Net.DNS
{
    /// <summary>
    /// Provides a soa record
    /// </summary>
    public struct SoaRecord
    {
        /// <summary>Parses the record using the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static SoaRecord Parse(DataReader reader)
        {
            SoaRecord result = new SoaRecord
            {
                MasterName = DomainName.Parse(reader),
                ResponsibleName = ParseEmailAddress(reader),
                SerialNumber = reader.ReadUInt32(),
                RefreshInterval = reader.ReadInt32(),
                RetryInterval = reader.ReadInt32(),
                ExpireInterval = reader.ReadInt32(),
                NegativeCachingTTL = reader.ReadInt32()
            };
            return result;
        }

        private static MailAddress ParseEmailAddress(DataReader reader)
        {
            long endposition = -1;
            List<string> parts = new List<string>();
            while (true)
            {
                byte b = reader.ReadByte();
                if (b == 0)
                {
                    // end of domain, RFC1035
                    if (endposition > 0)
                    {
                        reader.BaseStream.Position = endposition;
                    }

                    if (parts.Any(p => p.Contains("@")))
                    {
                        return new MailAddress(parts.Join("."));
                    }
                    return new MailAddress(parts[0] + "@" + parts.SubRange(1).Join("."));
                }
                if (b >= 192)
                {
                    // Pointer, RFC1035
                    int pointer = (b - 192) * 256 + reader.ReadByte();
                    //save position
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

                    StringBuilder sb = new StringBuilder();
                    sb.Append(@"\[x");
                    string suffix = "/" + length + "]";
                    do
                    {
                        b = reader.ReadByte();
                        if (length < 8)
                        {
                            b &= (byte)(0xff >> (8 - length));
                        }
                        sb.Append(b.ToString("x2"));
                        length = length - 8;
                    } while (length > 0);
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

        /// <summary>The master name</summary>
        public DomainName MasterName;

        /// <summary>The responsible name</summary>
        public MailAddress ResponsibleName;

        /// <summary>The serial number</summary>
        public uint SerialNumber;

        /// <summary>The refresh interval (seconds)</summary>
        public int RefreshInterval;

        /// <summary>The retry interval (seconds)</summary>
        public int RetryInterval;

        /// <summary>The expire interval (seconds)</summary>
        public int ExpireInterval;

        /// <summary>The negative caching TTL (seconds)</summary>
        public int NegativeCachingTTL;

        /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format(
                "{0}. {1}. (\n" +
                 "\t{2} ; serial\n" +
                 "\t{3} ; refresh\n" +
                 "\t{4} ; retry\n" +
                 "\t{5} ; expire\n" +
                 "\t{6} ) ; ttl", MasterName, ResponsibleName, SerialNumber, RefreshInterval, RetryInterval, ExpireInterval, NegativeCachingTTL);
        }
    }
}
