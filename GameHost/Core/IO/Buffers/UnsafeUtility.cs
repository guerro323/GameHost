﻿using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RevolutionSnapshot.Core.Buffers
{
	public static unsafe class UnsafeUtility
	{
		public static bool SameData<T>(ref T left, ref T right)
		{
			var size     = Unsafe.SizeOf<T>();
			var leftPtr  = (byte*) Unsafe.AsPointer(ref left);
			var rightPtr = (byte*) Unsafe.AsPointer(ref right);

			for (var i = 0; i < size; i++)
				if (leftPtr[i] != rightPtr[i])
					return false;
			return true;
		}
		
		public static bool SameData<T>(T left, T right)
		{
			return SameData(ref left, ref right);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void MemCpy(byte* dest, byte* source, int size)
		{
			Unsafe.CopyBlock(dest, source, (uint) size);
		}
		
		const int Threshold = 128;

		static readonly int PlatformWordSize     = IntPtr.Size;
		static readonly int PlatformWordSizeBits = PlatformWordSize * 8;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static unsafe void CopyMemory(byte* srcPtr, byte* dstPtr, int count)
		{
			const int u32Size  = sizeof(UInt32);
			const int u64Size  = sizeof(UInt64);
			const int u128Size = sizeof(UInt64) * 2;

			byte* srcEndPtr = srcPtr + count;
			
			if (PlatformWordSize == u32Size)
			{
				// 32-bit
				while (srcPtr + u64Size <= srcEndPtr)
				{
					*(UInt32*)dstPtr =  *(UInt32*)srcPtr;
					dstPtr           += u32Size;
					srcPtr           += u32Size;
					*(UInt32*)dstPtr =  *(UInt32*)srcPtr;
					dstPtr           += u32Size;
					srcPtr           += u32Size;
				}
			}
			else if (PlatformWordSize == u64Size)
			{
				// 64-bit            
				while (srcPtr + u128Size <= srcEndPtr)
				{
					*(UInt64*)dstPtr =  *(UInt64*)srcPtr;
					dstPtr           += u64Size;
					srcPtr           += u64Size;
					*(UInt64*)dstPtr =  *(UInt64*)srcPtr;
					dstPtr           += u64Size;
					srcPtr           += u64Size;
				}

				if (srcPtr + u64Size <= srcEndPtr)
				{
					*(UInt64*)dstPtr ^= *(UInt64*)srcPtr;
					dstPtr           += u64Size;
					srcPtr           += u64Size;
				}
			}

			if (srcPtr + u32Size <= srcEndPtr)
			{
				*(UInt32*)dstPtr =  *(UInt32*)srcPtr;
				dstPtr           += u32Size;
				srcPtr           += u32Size;
			}

			if (srcPtr + sizeof(UInt16) <= srcEndPtr)
			{
				*(UInt16*)dstPtr =  *(UInt16*)srcPtr;
				dstPtr           += sizeof(UInt16);
				srcPtr           += sizeof(UInt16);
			}

			if (srcPtr + 1 <= srcEndPtr)
			{
				*dstPtr = *srcPtr;
			}
		}
	}
}