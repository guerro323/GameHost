using System;
using System.IO;

namespace RevolutionSnapshot.Core.Buffers
{
	public class DataStreamWriter : Stream
	{
		public DataBufferWriter Buffer;

		public DataStreamWriter(DataBufferWriter buffer)
		{
			Buffer = buffer;
		}

		public override void Flush()
		{
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			Buffer.Span.Slice(offset, count).CopyTo(buffer);
			return count;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			Buffer.Length = (int) value;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			Buffer.WriteDataSafe(buffer.AsSpan(offset, count), default);
		}

		public override bool CanRead  => true;
		public override bool CanSeek  => false;
		public override bool CanWrite => true;
		public override long Length   => Buffer.Length;
		public override long Position { get; set; }
	}
}