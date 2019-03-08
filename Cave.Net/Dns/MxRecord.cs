using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>
    /// Provides a MaileXchange dns record.
    /// </summary>
    public struct MxRecord
    {
        /// <summary>Parses a record using the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        public static MxRecord Parse(DataReader reader)
        {
            var result = new MxRecord
            {
                Preference = reader.ReadUInt16(),
                ExchangeDomainName = DomainName.Parse(reader),
            };
            return result;
        }

        /// <summary>The preference value.</summary>
        public ushort Preference;

        /// <summary>The exchange domain name.</summary>
        public DomainName ExchangeDomainName;

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("{0}, {1}", Preference, ExchangeDomainName);
        }
    }
}
