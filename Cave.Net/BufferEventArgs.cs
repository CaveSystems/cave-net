﻿using System;

namespace Cave.Net
{
    /// <summary>
    /// Provides buffer information.
    /// </summary>
    public class BufferEventArgs : EventArgs
    {
        bool handled;

        /// <summary>
        /// Gets the full buffer instance.
        /// </summary>
        public byte[] Buffer { get; }

        /// <summary>
        /// Gets the start offset of data in <see cref="Buffer"/>.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// Gets the length of data in <see cref="Buffer"/>.
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the buffer has been handled. Further processing will be skipped.
        /// </summary>
        public bool Handled { get => handled; set => handled |= value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BufferEventArgs"/> class.
        /// </summary>
        /// <param name="buffer">buffer instance.</param>
        /// <param name="offset">offset of data.</param>
        /// <param name="length">length of data.</param>
        public BufferEventArgs(byte[] buffer, int offset, int length)
        {
            Buffer = buffer;
            Offset = offset;
            Length = length;
        }
    }
}
