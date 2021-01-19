using System;
using System.Runtime.InteropServices;

namespace GameHost.IO
{
	public class MarshalAllocator : IAllocator
	{
		public static readonly MarshalAllocator Default = new MarshalAllocator();

		private const long VALID_HEADER = 44556677;
		
		public AllocatedMemory Alloc(uint size)
		{
			var ptr = Marshal.AllocHGlobal((int) size);

			return new AllocatedMemory(this, VALID_HEADER, size, ptr);
		}

		public void Free(AllocatedMemory memory)
		{
			if (memory.Data != VALID_HEADER)
				throw new InvalidOperationException("Invalid Header");

			if (!memory.IsValid)
				throw new InvalidOperationException("Invalid data");
			
			if (memory.Allocator != this)
				throw new InvalidOperationException($"Freeing memory on the wrong allocator");

			Marshal.FreeHGlobal(memory.DataPtr);
		}
	}
}