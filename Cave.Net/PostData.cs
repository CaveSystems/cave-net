﻿using System;
using System.IO;
using System.Text;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides post data for <see cref="HttpConnection.Post(ConnectionString, System.Collections.Generic.IList{PostData}, ProgressCallback, ConnectionString?, object)"/>.
    /// </summary>
    public class PostData
    {
        /// <summary>
        /// Creates a new <see cref="PostData"/> instance using a local file.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="fullPath">Full path to access the file.</param>
        /// <param name="contentType">Content type to use (defaults to application/octet-stream)</param>
        /// <returns>Returns a new <see cref="PostData"/> instance.</returns>
        public static PostData FromFile(string name, string fullPath, string contentType = null)
        {
            var fileName = Path.GetFileName(fullPath);
            return new()
            {
                Source = File.OpenRead(fullPath),
                Name = name,
                FileName = fileName,
                ContentType = contentType ?? "application/octet-stream",
            };
        }

        /// <summary>
        /// Creates a new <see cref="PostData"/> instance using name value combination transmitted using utf-8 text/plain encoding.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="value">Content to send.</param>
        /// <returns>Returns a new <see cref="PostData"/> instance.</returns>
        public static PostData Text(string name, string value)
        {
            return new()
            {
                Source = new MemoryStream(Encoding.UTF8.GetBytes(value)),
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                ContentType = "text/plain;charset=UTF-8",
            };
        }

        /// <summary>
        /// Creates a new <see cref="PostData"/> instance using name value combination transmitted using utf-8 text/plain encoding.
        /// </summary>
        /// <param name="name">Parameter name.</param>
        /// <param name="data">Content to send.</param>
        /// <returns>Returns a new <see cref="PostData"/> instance.</returns>
        public static PostData Binary(string name, byte[] data)
        {
            return new()
            {
                Source = new MemoryStream(data),
                Name = name ?? throw new ArgumentNullException(nameof(name)),
                ContentType = "application/octet-stream",
            };
        }

        /// <summary>
        /// Source stream containing the data to be sent.
        /// </summary>
        protected Stream Source { get; set; }

        /// <summary>
        /// Creates a new <see cref="PostData"/> instance.
        /// </summary>
        protected PostData() { }

        /// <summary>
        /// Parameter name
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// File name. Set to null to skip file name transmission.
        /// </summary>
        public string FileName { get; protected set; }

        /// <summary>
        /// Content type to transmit.
        /// </summary>
        public string ContentType { get; protected set; }

        /// <summary>
        /// Writes this instance to a writer.
        /// </summary>
        /// <param name="writer">Writer to write to.</param>
        /// <param name="length">Length of data (defaults to -1 to send everything.)</param>
        /// <param name="callback">Callback to be called during copy or null.</param>
        /// <param name="userItem">The user item used at callback.</param>
        public virtual void WriteTo(DataWriter writer, long length = -1, ProgressCallback callback = null, object userItem = null)
        {
            writer.Write($"Content-Disposition: form-data; name=\"{Name}\"");
            if (FileName != null) writer.Write($"; filename=\"{FileName}\"");
            writer.WriteLine($"Content-Type: {ContentType}");
            writer.WriteLine($"Content-Transfer-Encoding: 8BIT");
            writer.WriteLine($"Content-Length: {Source.Length}");
            writer.WriteLine();
            Source.CopyBlocksTo(writer.BaseStream, length, callback, userItem);
        }
    }
}
