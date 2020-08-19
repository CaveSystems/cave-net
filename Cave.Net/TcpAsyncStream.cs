using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cave.IO;

namespace Cave.Net
{
    /// <summary>
    /// Provides a stream implementation for <see cref="TcpAsyncClient"/>.
    /// </summary>
    /// <remarks>All functions of this class are threadsafe.</remarks>
    public class TcpAsyncStream : Stream
    {
        readonly FifoBuffer sendBuffer = new FifoBuffer();
        readonly TcpAsyncClient client;
        bool asyncSendInProgress;

        void AsyncSendNext()
        {
            try
            {
                byte[] buffer;
                lock (sendBuffer)
                {
                    if (sendBuffer.Length == 0)
                    {
                        asyncSendInProgress = false;
                        Monitor.Pulse(sendBuffer);
                        return;
                    }
                    buffer = sendBuffer.Dequeue(sendBuffer.Length);
                }
                client.SendAsync(buffer, AsyncSendNext);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                asyncSendInProgress = false;
                client.OnError(ex);
                client.Close();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpAsyncStream"/> class.
        /// </summary>
        /// <param name="client">Client to be used by this stream.</param>
        public TcpAsyncStream(TcpAsyncClient client) => this.client = client;

        /// <summary>
        /// Gets or sets a value indicating whether the stream use direct writes on the clients socket for each call to <see cref="Write(byte[], int, int)"/>.
        /// Default is false buffering all writes.
        /// You need to set thit to true if you use the clients <see cref="TcpAsyncClient.Send(byte[])"/> function and stream writing.
        /// </summary>
        public bool DirectWrites { get; set; }

        /// <summary>
        /// Gets the number of bytes available for reading.
        /// </summary>
        public int Available
        {
            get
            {
                var buffer = client.ReceiveBuffer;
                lock (buffer)
                {
                    return buffer.Available;
                }
            }
        }

        /// <summary>
        /// Gets the number of bytes present at the send buffer when using <see cref="DirectWrites"/> == false (default).
        /// </summary>
        public int SendBufferLength
        {
            get
            {
                lock (sendBuffer)
                {
                    return sendBuffer.Length;
                }
            }
        }

        /// <summary>Gets or sets the amount of time, in milliseconds, that a read operation blocks waiting for data.</summary>
        /// <value>A Int32 that specifies the amount of time, in milliseconds, that will elapse before a read operation fails. The default value, <see cref="Timeout.Infinite"/>, specifies that the read operation does not time out.</value>
        public override int ReadTimeout
        {
            get => client.ReceiveTimeout;
            set => client.ReceiveTimeout = value;
        }

        /// <summary>Gets or sets the amount of time, in milliseconds, that a write operation blocks waiting for transmission.</summary>
        /// <value>A Int32 that specifies the amount of time, in milliseconds, that will elapse before a write operation fails. The default value, <see cref="Timeout.Infinite"/>, specifies that the write operation does not time out.</value>
        public override int WriteTimeout
        {
            get => client.ReceiveTimeout;
            set => client.SendTimeout = value;
        }

        /// <summary>
        /// Gets a value indicating whether the stream can be read or not. This is always true.
        /// </summary>
        public override bool CanRead => true;

        /// <summary>
        /// Gets a value indicating whether the stream can seek or not. This is always false.
        /// </summary>
        public override bool CanSeek => false;

        /// <summary>
        /// Gets a value indicating whether the stream can be written or not. This is always true.
        /// </summary>
        public override bool CanWrite => true;

        /// <summary>
        /// Gets the number of bytes received (<see cref="TcpAsyncClient.BytesReceived"/>).
        /// </summary>
        public override long Length => client.BytesReceived;

        /// <summary>
        /// Gets or sets the current read position at the buffers still present in memory.
        /// </summary>
        public override long Position
        {
            get
            {
                var buffer = client.ReceiveBuffer;
                lock (buffer)
                {
                    return buffer.Position;
                }
            }
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Waits until all buffered data is sent.
        /// </summary>
        public override void Flush()
        {
            if (DirectWrites)
            {
                return;
            }

            for (; ;)
            {
                lock (sendBuffer)
                {
                    if (!asyncSendInProgress && client.PendingAsyncSends == 0)
                    {
                        if (sendBuffer.Length > 0)
                        {
                            throw new Exception("SendAsync aborted!");
                        }
                        return;
                    }
                    if (!Monitor.Wait(sendBuffer, 1000))
                    {
                        if (!client.IsConnected)
                        {
                            throw new InvalidOperationException("Client diconnected!");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reads data from the the buffers. A maximum of count bytes is read but if less is available any number of bytes may be read.
        /// If no bytes are available the read method will block until at least one byte is available, the connection is closed or the timeout is reached.
        /// </summary>
        /// <param name="array">byte array to write data to.</param>
        /// <param name="offset">start offset at array to begin writing at.</param>
        /// <param name="count">number of bytes to read.</param>
        /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
        /// <exception cref="TimeoutException">A timeout occured while waiting for incoming data. (See <see cref="ReadTimeout"/>).</exception>
        public override int Read(byte[] array, int offset, int count)
        {
            var timeout = client.ReceiveTimeout > 0 ? DateTime.UtcNow + TimeSpan.FromMilliseconds(client.ReceiveTimeout) : DateTime.MaxValue;
            var buffer = client.ReceiveBuffer;
            lock (buffer)
            {
                while (true)
                {
                    if (buffer.Available > 0)
                    {
                        break;
                    }
                    if (!client.IsConnected)
                    {
                        return 0;
                    }
                    var waitTime = (int)Math.Min(1000, (timeout - DateTime.UtcNow).Ticks / TimeSpan.TicksPerMillisecond);
                    if (waitTime <= 0)
                    {
                        throw new TimeoutException();
                    }
                    Monitor.Wait(buffer, waitTime);
                }
                return buffer.Read(array, offset, count);
            }
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain the new position.</param>
        /// <returns>The new position within the current stream.</returns>
        /// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("The stream does not support seeking.");

        /// <summary>
        /// Not supported.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="NotSupportedException">The stream does not support both writing and seeking.</exception>
        public override void SetLength(long value) => throw new NotSupportedException("The stream does not support both writing and seeking.");

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">An array of bytes. This method copies <paramref name="count"/> bytes from <paramref name="buffer"/> to the current stream.</param>
        /// <param name="offset">The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes to the current stream.</param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (DirectWrites)
            {
                client.Send(buffer, offset, count);
                return;
            }

            lock (sendBuffer)
            {
                sendBuffer.Enqueue(buffer, offset, count);
                if (!asyncSendInProgress)
                {
                    asyncSendInProgress = true;
                    Task.Factory.StartNew(AsyncSendNext);
                }
            }
        }

#if NETSTANDARD13
        /// <summary>
        /// Closes the tcp connection
        /// </summary>
        public virtual void Close()
        {
            if (client.IsConnected)
            {
                Flush();
                client.Close();
            }
        }
#else
        /// <summary>
        /// Closes the tcp connection.
        /// </summary>
        public override void Close()
        {
            if (client.IsConnected)
            {
                Flush();
                client.Close();
            }
            base.Close();
        }
#endif
    }
}
