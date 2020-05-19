using System;
using System.Runtime.CompilerServices;

namespace GameHost.Core
{
    public unsafe ref struct StructBuffer<T>
        where T : unmanaged
    {
        private readonly Span<byte> pointer;
        
        public int Length;
        public int Capacity => pointer.Length;
        public Span<T> AsSpan => new Span<T>(Unsafe.AsPointer(ref pointer.GetPinnableReference()), pointer.Length);
        
        public StructBuffer(Span<byte> pointer)
        {
            this.pointer = pointer;
            Length       = 0;
        }

        public ref T this[int index]
        {
            get
            {
                if (index > Capacity)
                    throw new IndexOutOfRangeException($"{index} > {Capacity}");
                return ref Unsafe.AsRef<T>(Unsafe.AsPointer(ref pointer.GetPinnableReference()));
            }
        }

        public void Add(T value)
        {
            this[Length++] = value;
        }

        public void RemoveAtSwapBack(int index)
        {
            this[index] = this[Length - 1];
            Length--;
        }

        public void Clear()
        {
            Length = 0;
        }
    }
}
