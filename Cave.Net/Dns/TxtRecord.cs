using System.Collections.Generic;
using System.Text;
using Cave.IO;

namespace Cave.Net.DNS
{
    /// <summary>
    /// Provides a txt record
    /// </summary>
    public class TxtRecord
    {
        /// <summary>Parses the record using the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static TxtRecord Parse(DataReader reader, int length)
        {
            long end = reader.BaseStream.Position + length;
            TxtRecord result = new TxtRecord();
            List<string> parts = new List<string>();
            while (reader.BaseStream.Position < end)
            {
                int len = reader.ReadByte();
                parts.Add(reader.ReadString(len));
            }
            result.Parts = parts.ToArray();
            return result;
        }

        /// <summary>Gets the parts.</summary>
        /// <value>The parts.</value>
        public string[] Parts { get; private set; }

        /// <summary>Returns a <see cref="System.String" /> that represents this instance.</summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            foreach (string part in Parts)
            {
                if (result.Length > 0)
                {
                    result.Append(' ');
                }

                if (part.IndexOfAny(new char[] { ' ', '"' }) > -1)
                {
                    result.Append('"');
                    result.Append(part.Replace("\"", "\"\""));
                    result.Append('"');
                }
                else
                {
                    result.Append(part);
                }
            }
            return result.ToString();
        }
    }
}