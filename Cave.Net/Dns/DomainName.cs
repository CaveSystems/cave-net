using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cave.Collections;
using Cave.IO;

namespace Cave
{
    /// <summary>
    /// Provides domain name parsing
    /// </summary>
    public sealed class DomainName
    {
        /// <summary>Performs an implicit conversion from <see cref="string"/> to <see cref="DomainName"/>.</summary>
        /// <param name="value">The value.</param>
        /// <returns>The result of the conversion.</returns>
        /// <exception cref="InvalidDataException"></exception>
        public static implicit operator DomainName(string value)
        {
            if (!TryParse(value, out DomainName name))
            {
                throw new InvalidDataException(string.Format("{0} is not a valid domain name!", value));
            }

            return name;
        }

        /// <summary>Implements the operator ==.</summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(DomainName a, DomainName b)
        {
            if (ReferenceEquals(b, null))
            {
                return ReferenceEquals(a, null);
            }

            if (ReferenceEquals(a, null))
            {
                return false;
            }

            return DefaultComparer.Equals(a.Parts, b.Parts);
        }

        /// <summary>Implements the operator !=.</summary>
        /// <param name="a">a.</param>
        /// <param name="b">The b.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(DomainName a, DomainName b)
        {
            if (ReferenceEquals(b, null))
            {
                return !ReferenceEquals(a, null);
            }

            if (ReferenceEquals(a, null))
            {
                return true;
            }

            return !DefaultComparer.Equals(a.Parts, b.Parts);
        }

        static readonly IdnMapping s_IdnParser = new IdnMapping() { UseStd3AsciiRules = true };
        static readonly Regex s_AsciiNameRegex = new Regex("^[a-zA-Z0-9_-]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        static bool TryParsePart(string value, out string label)
        {
            try
            {
                if (s_AsciiNameRegex.IsMatch(value))
                {
                    label = value;
                    return true;
                }
                else
                {
                    label = s_IdnParser.GetAscii(value);
                    return true;
                }
            }
            catch
            {
                label = null;
                return false;
            }
        }

        /// <summary>Tries to parse a domain name</summary>
        /// <param name="value">The value.</param>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public static bool TryParse(string value, out DomainName name)
        {
            if (value == ".")
            {
                name = Root;
                return true;
            }

            List<string> parts = new List<string>();
            int start = 0;
            string part;
            for (int i = 0; i < value.Length; ++i)
            {
                if (value[i] == '.' && (i == 0 || value[i - 1] != '\\'))
                {
                    if (TryParsePart(value.Substring(start, i - start), out part) && (part.Length <= 64))
                    {
                        parts.Add(part);
                        start = i + 1;
                    }
                    else
                    {
                        name = null;
                        return false;
                    }
                }
            }

            if (value.Length == start)
            {
                // empty label --> name ends with dot
            }
            else if (TryParsePart(value.Substring(start, value.Length - start), out part) && (part.Length <= 64))
            {
                parts.Add(part);
            }
            else
            {
                name = null;
                return false;
            }

            name = new DomainName(parts.ToArray());
            return true;
        }

        /// <summary>Parses a domain name using the specified reader.</summary>
        /// <param name="reader">The reader.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException">Unsupported extended dns label</exception>
        public static DomainName Parse(DataReader reader)
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

                    return new DomainName(parts.ToArray());
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

        /// <summary>The DNS root name (.)</summary>
        /// <value>The root.</value>
		public static DomainName Root { get; } = new DomainName(new string[] { });

        /// <summary>Gets the parts of the domain name.</summary>
        /// <value>The parts.</value>
        public string[] Parts { get; private set; }

        /// <summary>Creates a new instance of the DomainName class</summary>
        /// <param name="parts">The parts of the DomainName</param>
        private DomainName(string[] parts)
        {
            Parts = parts;
        }

        /// <summary>Gets the parent zone of the domain name.</summary>
        /// <returns>The DomainName of the parent zone</returns>
        /// <exception cref="Exception">No parent available.</exception>
        public DomainName GetParent()
        {
            if (Parts.Length == 0)
            {
                throw new Exception("No parent available.");
            }

            string[] parts = new string[Parts.Length - 1];
            Array.Copy(Parts, 1, parts, 0, parts.Length);
            return new DomainName(parts);
        }

        /// <summary>Randomizes the character casing.</summary>
        /// <returns></returns>
        public DomainName RandomCase()
        {
            string[] parts = Parts;
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = StringExtensions.RandomCase(parts[i]);
            }
            return new DomainName(parts);
        }

        /// <summary>Returns a <see cref="string" /> that represents this instance.</summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Join(".", Parts);
        }

        /// <summary>Returns a hash code for this instance.</summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hash = Parts.Length.GetHashCode();
            foreach (string p in Parts)
            {
                hash ^= p.GetHashCode();
            }

            return hash;
        }

        /// <summary>Determines whether the specified <see cref="object" />, is equal to this instance.</summary>
        /// <param name="obj">The <see cref="object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            DomainName other = obj as DomainName;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return DefaultComparer.Equals(Parts, other.Parts);
        }
    }
}
