using System;
using System.Runtime.CompilerServices;

namespace GameHost.Simulation.Utility
{
    public static unsafe class UnsafeUtility
    {
        private const int Threshold = 128;

        private static readonly int PlatformWordSize = IntPtr.Size;
        private static readonly int PlatformWordSizeBits = PlatformWordSize * 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SameData<T>(ref T left, ref T right)
        {
            var size = Unsafe.SizeOf<T>();
            var leftPtr = (byte*) Unsafe.AsPointer(ref left);
            var rightPtr = (byte*) Unsafe.AsPointer(ref right);

            for (var i = 0; i < size; i++)
                if (leftPtr[i] != rightPtr[i])
                    return false;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SameData<T>(T left, T right)
        {
            return SameData(ref left, ref right);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemCpy(byte* dest, byte* source, int size)
        {
            Unsafe.CopyBlock(dest, source, (uint) size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CopyMemory(byte* srcPtr, byte* dstPtr, int count)
        {
            const int u32Size = sizeof(uint);
            const int u64Size = sizeof(ulong);
            const int u128Size = sizeof(ulong) * 2;

            var srcEndPtr = srcPtr + count;

            if (PlatformWordSize == u32Size)
            {
                // 32-bit
                while (srcPtr + u64Size <= srcEndPtr)
                {
                    *(uint*) dstPtr = *(uint*) srcPtr;
                    dstPtr += u32Size;
                    srcPtr += u32Size;
                    *(uint*) dstPtr = *(uint*) srcPtr;
                    dstPtr += u32Size;
                    srcPtr += u32Size;
                }
            }
            else if (PlatformWordSize == u64Size)
            {
                // 64-bit            
                while (srcPtr + u128Size <= srcEndPtr)
                {
                    *(ulong*) dstPtr = *(ulong*) srcPtr;
                    dstPtr += u64Size;
                    srcPtr += u64Size;
                    *(ulong*) dstPtr = *(ulong*) srcPtr;
                    dstPtr += u64Size;
                    srcPtr += u64Size;
                }

                if (srcPtr + u64Size <= srcEndPtr)
                {
                    *(ulong*) dstPtr ^= *(ulong*) srcPtr;
                    dstPtr += u64Size;
                    srcPtr += u64Size;
                }
            }

            if (srcPtr + u32Size <= srcEndPtr)
            {
                *(uint*) dstPtr = *(uint*) srcPtr;
                dstPtr += u32Size;
                srcPtr += u32Size;
            }

            if (srcPtr + sizeof(ushort) <= srcEndPtr)
            {
                *(ushort*) dstPtr = *(ushort*) srcPtr;
                dstPtr += sizeof(ushort);
                srcPtr += sizeof(ushort);
            }

            if (srcPtr + 1 <= srcEndPtr) *dstPtr = *srcPtr;
        }
    }
}