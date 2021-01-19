using System;
using System.Buffers;

namespace GameHost.IO
{
	public struct DisposableArray : IDisposable
	{
		private byte[] bytes;

		public static DisposableArray Rent(int size, out byte[] bytes)
		{
			return new() {bytes = (bytes = ArrayPool<byte>.Shared.Rent(size))};
		}

		public void Dispose()
		{
			ArrayPool<byte>.Shared.Return(bytes);
		}
	}
}