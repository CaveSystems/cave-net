﻿using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>
    /// Provides a dns query.
    /// </summary>
    public struct DnsQuery
    {
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

        /// <summary>The name.</summary>
        public DomainName Name;

        /// <summary>The record type.</summary>
        public DnsRecordType RecordType;

        /// <summary>The record class.</summary>
        public DnsRecordClass RecordClass;

        /// <summary>The flags.</summary>
        public DnsFlags Flags;

        internal void Write(DataWriter writer)
        {
            var name = Name;
            while (name != DomainName.Root)
            {
                writer.WritePrefixed(name.Parts[0]);
                name = name.GetParent();
            }
            writer.Write((byte)0);
            writer.Write((ushort)RecordType);
            writer.Write((ushort)RecordClass);
        }

        /// <summary>Randomizes the case.</summary>
        public void RandomizeCase() => Name = Name.RandomCase();

        /// <summary>Determines whether the specified <see cref="object" />, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => obj is DnsQuery other && this == other;

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString() => RecordClass + " " + RecordType + " " + Name;

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.
        /// </returns>
        public override int GetHashCode() => RecordClass.GetHashCode() ^ RecordType.GetHashCode() ^ Name.GetHashCode() ^ Flags.GetHashCode();
    }
}
