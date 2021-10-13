using System.Collections.Generic;
using System.IO;
using System.Net;
using Cave.IO;

namespace Cave.Net.Dns
{
    /// <summary>
    /// Provides a DnsAnswer.
    /// </summary>
    public class DnsResponse
    {
        readonly ushort flags;

        /// <summary>Gets the sender.</summary>
        /// <value>The sender.</value>
        public IPAddress Sender { get; }

        /// <summary>Gets the queries.</summary>
        /// <value>The queries.</value>
        public IList<DnsQuery> Queries { get; }

        /// <summary>Gets the answers.</summary>
        /// <value>The answers.</value>
        public IList<DnsRecord> Answers { get; }

        /// <summary>Gets the authorities.</summary>
        /// <value>The authorities.</value>
        public IList<DnsRecord> Authorities { get; }

        /// <summary>Gets the additional records.</summary>
        /// <value>The additional records.</value>
        public IList<DnsRecord> AdditionalRecords { get; }

        /// <summary>Initializes a new instance of the <see cref="DnsResponse"/> class.</summary>
        /// <param name="srv">The server.</param>
        /// <param name="data">The data.</param>
        internal DnsResponse(IPAddress srv, byte[] data)
        {
            Sender = srv;
            using var ms = new MemoryStream(data);
            var reader = new DataReader(ms, endian: EndianType.BigEndian);
            TransactionID = reader.ReadUInt16();
            flags = reader.ReadUInt16();
            int queryCount = reader.ReadUInt16();
            int answerRecordCount = reader.ReadUInt16();
            int authorityRecordCount = reader.ReadUInt16();
            int additionalRecordCount = reader.ReadUInt16();

            Queries = LoadQueries(reader, queryCount);
            Answers = LoadRecords(reader, answerRecordCount);
            Authorities = LoadRecords(reader, authorityRecordCount);
            AdditionalRecords = LoadRecords(reader, additionalRecordCount);

            // TODO signature
        }

        /// <summary>Loads the records.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="recordCount">The record count.</param>
        /// <returns>Returns a list of <see cref="DnsRecord"/>s.</returns>
        IList<DnsRecord> LoadRecords(DataReader reader, int recordCount)
        {
            var result = new List<DnsRecord>(recordCount);
            for (var i = 0; i < recordCount; i++)
            {
                var item = DnsRecord.Parse(reader);
                result.Add(item);
            }
            return result.AsReadOnly();
        }

        IList<DnsQuery> LoadQueries(DataReader reader, int queryCount)
        {
            var result = new List<DnsQuery>(queryCount);
            for (var i = 0; i < queryCount; i++)
            {
                var item = DnsQuery.Parse(reader);
                result.Add(item);
            }
            return result.AsReadOnly();
        }

        /// <summary>Gets the transaction identifier.</summary>
        /// <value>The transaction identifier.</value>
        public int TransactionID { get; private set; }

        /// <summary>Gets the flags.</summary>
        /// <value>The flags.</value>
        public DnsFlags Flags => (DnsFlags)flags & DnsFlags.MaskFlags;

        /// <summary>Gets the response code.</summary>
        /// <value>The response code.</value>
        public DnsResponseCode ResponseCode => (DnsResponseCode)(flags & (int)DnsFlags.MaskResponseCode);

        /// <summary>Gets a value indicating whether this instance is an authoritive answer.</summary>
        /// <value>
        /// <c>true</c> if this instance is authoritive answer; otherwise, <c>false</c>.
        /// </value>
        public bool IsAuthoritiveAnswer => ((DnsFlags)flags & DnsFlags.AuthoritiveAnswer) != 0;

        /// <summary>Gets a value indicating whether this instance is a truncated response.</summary>
        /// <value>
        /// <c>true</c> if this instance is truncated; otherwise, <c>false</c>.
        /// </value>
        public bool IsTruncatedResponse => ((DnsFlags)flags & DnsFlags.TruncatedResponse) != 0;

        /// <summary>Gets a value indicating whether recursion is desired.</summary>
        /// <value>
        /// <c>true</c> if this instance is recursion desired; otherwise, <c>false</c>.
        /// </value>
        public bool IsRecursionDesired => ((DnsFlags)flags & DnsFlags.RecursionDesired) != 0;

        /// <summary>Gets a value indicating whether recursion is available.</summary>
        /// <value>
        /// <c>true</c> if this instance is recursion allowed; otherwise, <c>false</c>.
        /// </value>
        public bool IsRecursionAvailable => ((DnsFlags)flags & DnsFlags.RecursionAvailable) != 0;

        /// <summary>Gets a value indicating whether this instance is authentic data.</summary>
        /// <value>
        /// <c>true</c> if this instance is authentic data; otherwise, <c>false</c>.
        /// </value>
        public bool IsAuthenticData => ((DnsFlags)flags & DnsFlags.AuthenticData) != 0;

        /// <summary>Gets a value indicating whether this instance is checking disabled.</summary>
        /// <value>
        /// <c>true</c> if this instance is checking disabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsCheckingDisabled => ((DnsFlags)flags & DnsFlags.CheckingDisabled) != 0;

        /// <summary>Gets the name of the log source.</summary>
        /// <value>The name of the log source.</value>
        public string LogSourceName => "DnsResponse";
    }
}
