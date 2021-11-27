using System.IO;
using System.Linq;
using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>Provides a dns query.</summary>
    public struct DnsQuery
    {
        #region Public Fields

        /// <summary>The flags.</summary>
        public DnsFlags Flags;

        /// <summary>The name.</summary>
        public DomainName Name;

        /// <summary>The record class.</summary>
        public DnsRecordClass RecordClass;

        /// <summary>The record type.</summary>
        public DnsRecordType RecordType;

        #endregion Public Fields

        #region Public Properties

        /// <summary>Gets the length of the query in bytes. (An udp query has to be smaller than or equal to 512 bytes.)</summary>
        public int Length =>
            4   // transaction id
            + 2 // flags
            + 2 // question records
            + 2 // answer records
            + 2 // authority records
            + 2 // additional records
            + Name.Parts.Select(p => p.Length + 1).Sum() // name parts
            + 2 // record type
            + 2;

        #endregion Public Properties

        #region Public Methods

        /// <summary>Implements the operator !=.</summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DnsQuery a, DnsQuery b)
        {
            return a.Name != b.Name
                || a.RecordClass != b.RecordClass
                || a.RecordType != b.RecordType;
        }

        /// <summary>Implements the operator ==.</summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DnsQuery a, DnsQuery b)
        {
            return a.Name == b.Name
                && a.RecordClass == b.RecordClass
                && a.RecordType == b.RecordType;
        }

        /// <summary>Parses the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <returns>Returns a new <see cref="DnsQuery"/> structure.</returns>
        public static DnsQuery Parse(DataReader reader)
        {
            var result = new DnsQuery
            {
                Name = DomainName.Parse(reader),
                RecordType = (DnsRecordType)reader.ReadUInt16(),
                RecordClass = (DnsRecordClass)reader.ReadUInt16(),
            };
            return result;
        }

        // record class

        /// <summary>Determines whether the specified <see cref="object"/>, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj) => obj is DnsQuery other && this == other;

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode() => RecordClass.GetHashCode() ^ RecordType.GetHashCode() ^ Name.GetHashCode() ^ Flags.GetHashCode();

        /// <summary>Randomizes the case.</summary>
        public void RandomizeCase() => Name = Name.RandomCase();

        /// <summary>Gets the query as byte[] array.</summary>
        /// <param name="transactionId">The transaction identifer.</param>
        /// <returns>Returns a new byte array containing the query data.</returns>
        public byte[] ToArray(ushort transactionId)
        {
            using var stream = new MemoryStream();
            var writer = new DataWriter(stream, StringEncoding.ASCII, endian: EndianType.BigEndian);
            writer.Write(transactionId); // transaction id
            writer.Write((ushort)Flags); // flags
            writer.Write((ushort)1); // question records
            writer.Write((ushort)0); // answer records
            writer.Write((ushort)0); // authority records
            writer.Write((ushort)0); // additional records
            var name = Name;
            foreach (var part in name.Parts)
            {
                writer.WritePrefixed(part);
            }
            /*
            while (name != DomainName.Root)
            {
                writer.WritePrefixed(name.Parts[0]);
                name = name.GetParent();
            }*/
            writer.Write((byte)0);
            writer.Write((ushort)RecordType);
            writer.Write((ushort)RecordClass);
            return stream.ToArray();
        }

        /// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
        /// <returns>A <see cref="string"/> that represents this instance.</returns>
        public override string ToString() => RecordClass + " " + RecordType + " " + Name;

        #endregion Public Methods
    }
}
