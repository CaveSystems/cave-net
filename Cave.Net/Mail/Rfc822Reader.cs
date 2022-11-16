using System;

namespace Cave.Mail
{
    /// <summary>
    /// Provides a mail reader used to read rfc 822.
    /// </summary>
    public class Rfc822Reader
    {
        readonly byte[] data;
        int position;

        /// <summary>
        /// Extracts a byte buffer with the specified length starting at the specified position.
        /// </summary>
        /// <param name="start"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] Extract(int start, int count)
        {
            var result = new byte[count];
            Buffer.BlockCopy(data, start, result, 0, count);
            return result;
        }

        /// <summary>
        /// Creates a new <see cref="Rfc822Reader"/> from the specified data.
        /// </summary>
        /// <param name="data"></param>
        public Rfc822Reader(byte[] data) => this.data = data;

        /// <summary>
        /// Peeks at the next byte. Result is -1 if no more bytes available.
        /// </summary>
        /// <returns></returns>
        public virtual int Peek()
        {
            if (position >= data.Length)
            {
                return -1;
            }

            return data[position];
        }

        /// <summary>
        /// Reads the next byte. Result is -1 if no more bytes available.
        /// </summary>
        /// <returns></returns>
        public virtual int Read()
        {
            if (position >= data.Length)
            {
                return -1;
            }

            return data[position++];
        }

        /// <summary>
        /// Reads a string block with the specified length.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public string ReadBlock(int count)
        {
            var result = ASCII.GetString(data, position, count);
            position += count;
            return result;
        }

        /// <summary>
        /// Readss a byte block with the specified length.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public byte[] ReadBlockData(int count)
        {
            var result = Extract(position, count);
            position += count;
            return result;
        }

        /// <summary>
        /// Reads a string with all bytes until the end is reached.
        /// </summary>
        /// <returns></returns>
        public virtual string ReadToEnd()
        {
            var result = ASCII.GetString(data, position, data.Length - position);
            position = data.Length;
            return result;
        }

        /// <summary>
        /// Reads a byte buffer with all bytes until the end is reached.
        /// </summary>
        /// <returns></returns>
        public byte[] ReadToEndData()
        {
            var start = position;
            position = data.Length;
            return Extract(start, position - start);
        }

        /// <summary>
        /// Reads a line with the specified encoding without the trailing CR, LF, CRLF.
        /// </summary>
        /// <returns></returns>
        public virtual string ReadLine()
        {
            var start = position;
            while (position < data.Length)
            {
                switch (data[position])
                {
                    case 0x0D:
                    {
                        var end = position;
                        position += 1;
                        if (data[position] == 0x0A) { position += 1; }
                        return Rfc2047.DefaultEncoding.GetString(data, start, end - start);
                    }
                    case 0x0A:
                    {
                        var end = position;
                        position += 1;
                        return Rfc2047.DefaultEncoding.GetString(data, start, end - start);
                    }
                }
                position++;
            }
            var size = data.Length - start;
            if (size == 0)
            {
                return null;
            }

            return ASCII.GetString(data, start, size);
        }

        /// <summary>
        /// Provides the current position in the reader.
        /// </summary>
        public int Position { get => position; set { if ((value >= data.Length) || (value < 0)) { throw new ArgumentOutOfRangeException(nameof(value)); } position = value; } }

        /// <summary>
        /// Provides the overall length of data this reader works on.
        /// </summary>
        public int Length => data.Length;
    }
}
