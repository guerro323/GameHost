using System;
using System.Runtime.CompilerServices;
using GameHost.Native.Fixed;

namespace GameHost.Native
{
	public unsafe struct FixedBuffer256<T> : IFixedBuffer<T>, IEquatable<FixedBuffer256<T>>
		where T : IEquatable<T>
	{
		private const int  ConstCapacity = 256;
		private fixed byte buffer[ConstCapacity];

		int IFixedBuffer.Capacity => ConstCapacity / Unsafe.SizeOf<T>();
		int IFixedBuffer.Length   { get; set; }

		Span<byte> IFixedBuffer.Raw
		{
			get
			{
				fixed (byte* ptr = buffer)
					return new Span<byte>(ptr, this.GetLength() * Unsafe.SizeOf<T>());
			}
		}
		
		public Span<T> Span
		{
			get
			{
				fixed (byte* ptr = buffer)
					return new Span<T>(ptr, this.GetLength());
			}
		}

		public bool Equals(FixedBuffer256<T> other)
		{
			return Span.SequenceEqual(other.Span);
		}

		public override bool Equals(object obj)
		{
			return obj is FixedBuffer256<T> other && Equals(other);
		}

		public override int GetHashCode()
		{
			return FixedBufferUtility.ComputeHashCode(Span);
		}
	}
}