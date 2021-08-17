using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace GameHost.IO
{
	public class ManagedAllocator : IAllocator
	{
		public static readonly ManagedAllocator Default = new ManagedAllocator();

		static ManagedAllocator()
		{
#if NET
			static void unload(AssemblyLoadContext ctx)
			{
				ctx.Unloading -= unload;
				
				foreach (var handle in Default.handles)
				{
					GCHandle.FromIntPtr(handle.Key)
					        .Free();
				}

				Default.handles.Clear();
			}

			AssemblyLoadContext.Default.Unloading += unload;
#endif
		}

		private readonly ArrayPool<byte>         pool;
		private readonly ConcurrentDictionary<IntPtr, byte> handles;

		public ManagedAllocator()
		{
			pool    = ArrayPool<byte>.Shared;
			handles = new ConcurrentDictionary<IntPtr, byte>();
		}

		public AllocatedMemory Alloc(uint size)
		{
			var rented = pool.Rent((int) size);
			rented.AsSpan(0, (int) size)
			      .Clear();

			var handle = GCHandle.Alloc(rented, GCHandleType.Pinned);
			handles.TryAdd(GCHandle.ToIntPtr(handle), 0);

			return new AllocatedMemory(this, GCHandle.ToIntPtr(handle).ToInt64(), size, handle.AddrOfPinnedObject());
		}

		public GCHandle getHandle(AllocatedMemory memory)
		{
			var handle = GCHandle.FromIntPtr(new IntPtr(memory.Data));
			if (!handle.IsAllocated)
				throw new InvalidOperationException("unallocated_handle");
			
			return handle;
		}

		public void freeHandle(GCHandle handle)
		{
			handle.Free();
			handles.TryRemove(GCHandle.ToIntPtr(handle), out _);
		}

		public void Free(AllocatedMemory memory)
		{
			if (!memory.IsValid)
				throw new InvalidOperationException("invalid_memory");
			if (memory.Allocator != this)
				throw new InvalidOperationException("invalid_allocator");
			if (memory.Data == 0)
				throw new InvalidOperationException("null_ptr");

			var handle = getHandle(memory);
			if (!handles.ContainsKey((IntPtr) memory.Data))
				throw new InvalidOperationException("Such handles are not allocated!");

			try
			{
				if (handle.Target is not byte[] rented)
					throw new InvalidOperationException("invalid_data");

				if (rented.Length < memory.Size)
					throw new InvalidOperationException("invalid_data_size");

				pool.Return(rented);
			}
			finally
			{
				freeHandle(handle);
			}
		}
	}
}