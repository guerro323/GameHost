using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameHost.IO
{
	public interface IAllocator
	{
		AllocatedMemory Alloc(uint size);
		void            Free(AllocatedMemory memory);
	}

	public struct AllocatedMemory : IDisposable
	{
		private const int VALID_HEADER = 123456789;
		
		public bool IsValid => allocatorHandle.IsAllocated && header == VALID_HEADER
			                            && Data != 0 && DataPtr != IntPtr.Zero;

		private readonly int      header;
		private readonly GCHandle allocatorHandle;

		public IAllocator Allocator { get; set; }

		public readonly long       Data;
		public readonly uint       Size;

		public readonly IntPtr DataPtr;

		public unsafe Span<byte> Span
		{
			get
			{
				if (!IsValid)
					throw new InvalidOperationException("Not Valid");
				return new Span<byte>(DataPtr.ToPointer(), (int) Size);
			}
		}

		public unsafe ref T As<T>()
		{
			if (!IsValid)
				throw new InvalidOperationException("Not Valid");
			
			if (Unsafe.SizeOf<T>() != Size)
				throw new InvalidOperationException("size_mismatch");
			
			return ref Unsafe.AsRef<T>(DataPtr.ToPointer());
		}

		public AllocatedMemory(IAllocator allocator, in long data, in uint size, in IntPtr dataPtr)
		{
			header = VALID_HEADER;
			
			allocatorHandle = GCHandle.Alloc(allocator, GCHandleType.WeakTrackResurrection);
			
			Allocator = allocator;
			Data      = data;
			Size      = size;

			DataPtr = dataPtr;
		}

		public void Dispose()
		{
			if (!IsValid)
				throw new InvalidOperationException("Not Valid");
			
			allocatorHandle.Free();
			
			Allocator.Free(this);
		}
	}
}