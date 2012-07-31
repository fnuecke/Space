using System;
using System.Collections.Generic;
using System.IO;

namespace Engine.IO
{
    /// <summary>
    /// Simple FIFO queue like stream. This implementation is NOT thread-safe!
    /// </summary>
    public class SlidingStream : Stream
    {
        #region Properties

        public override bool CanRead { get { return true; } }

        public override bool CanSeek { get { return false; } }

        public override bool CanWrite { get { return true; } }

        public bool DataAvailable { get { return _pendingSegments.Count > 0; } }

        public override long Length { get { throw new NotSupportedException(); } }

        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        #endregion

        #region Fields

        /// <summary>
        /// Internal representation of this stream.
        /// </summary>
        private readonly LinkedList<ArraySegment<byte>> _pendingSegments = new LinkedList<ArraySegment<byte>>();

        /// <summary>
        /// Remember if this stream is closed, because if it is we throw exceptions on read/write.
        /// </summary>
        private bool _closed;

        #endregion

        protected override void Dispose(bool disposing)
        {
            _closed = true;
            _pendingSegments.Clear();

            base.Dispose(disposing);
        }

        #region Read / Write
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_closed)
            {
                throw new IOException("Stream has been closed.");
            }

            var currentCount = 0;

            // Read until we read all we're allowed to, or have nothing left.
            while (currentCount != count && _pendingSegments.Count > 0)
            {
                // Get the first available segment.
                var segment = _pendingSegments.First.Value;
                _pendingSegments.RemoveFirst();

                // Copy from it as much as we need or as much as we can.
                var toCopy = System.Math.Min(segment.Count, count - currentCount);
                Array.Copy(segment.Array, segment.Offset, buffer, offset + currentCount, toCopy);
                currentCount += toCopy;

                // If we didn't use up the segment, push what remains back to the list.
                if (segment.Count > toCopy)
                {
                    _pendingSegments.AddFirst(new ArraySegment<byte>(segment.Array, segment.Offset + toCopy, segment.Count - toCopy));
                }
            }

            return currentCount;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (_closed)
            {
                throw new IOException("Stream has been closed.");
            }

            var copy = new byte[count];
            Array.Copy(buffer, offset, copy, 0, count);
            _pendingSegments.AddLast(new ArraySegment<byte>(copy));
        }

        public override void Flush()
        {
            // Nothing to do.
        }

        #endregion

        #region Not supported

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        #endregion
    }
}
