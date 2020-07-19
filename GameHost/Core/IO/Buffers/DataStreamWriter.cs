using System;
using System.IO;

namespace RevolutionSnapshot.Core.Buffers
{
	public unsafe class DataStreamWriter : Stream
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
			new Span<byte>((void*) (Buffer.GetSafePtr() + offset), count).CopyTo(buffer);
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
			fixed (byte* ptr = buffer)
				Buffer.WriteDataSafe(ptr + offset, count, default);
		}

		public override bool CanRead  => true;
		public override bool CanSeek  => false;
		public override bool CanWrite => true;
		public override long Length   => Buffer.Length;
		public override long Position { get; set; }
	}
}