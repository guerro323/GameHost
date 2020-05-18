using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace package.stormiumteam.networking.runtime.lowlevel
{
	public static unsafe class UnsafeUtility
	{
		public static void* Malloc(int size)
		{
			return (void*) Marshal.AllocHGlobal(size);
		}

		public static void MemCpy(void* addr1, void* addr2, int size)
		{
			Unsafe.CopyBlock(addr1, addr2, (uint) size);
		}

		public static void Free(void* addr)
		{
			Marshal.FreeHGlobal((IntPtr) addr);
		}
	}
}