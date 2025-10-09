using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraLib.Pixel.Formats
{
    internal sealed class ConcatStream : Stream
    {
        private readonly Queue<Stream> sources;
        private Stream current;

        public ConcatStream() : this(Stream.Null)
        { }

        public ConcatStream(Stream stream)
        {
            sources = new Queue<Stream>();
            current = stream;
        }

        public ConcatStream(IEnumerable<Stream> streams)
        {
            sources = new Queue<Stream>(streams);
            current = sources.Dequeue();
        }

        public void Enqueue(Stream stream) =>
            sources.Enqueue(stream);

        public override int Read(byte[] buffer, int offset, int count)
#if NET6_0_OR_GREATER
            => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> bytes)
        {
            int read = current.Read(bytes);
            if (read == 0 && sources.Count > 0)
            {
                current.Dispose();
                current = sources.Dequeue();
                return Read(bytes);
            }
            return read;
        }
#else
        {
            int read = current.Read(buffer, offset, count);
            if (read == 0 && sources.Count > 0)
            {
                current.Dispose();
                current = sources.Dequeue();
                return Read(buffer, offset, count);
            }
            return read;
        }
#endif

        public override bool CanRead => current.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            current.Dispose();
            while (sources.Count > 0)
            {
                current = sources.Dequeue();
                current.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
