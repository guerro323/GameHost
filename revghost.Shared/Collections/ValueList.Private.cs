using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace revghost.Shared.Collections;

public partial struct ValueList<T>
{
    private struct Controller
    {
        public T[]? Data = Array.Empty<T>();
        public int Count;

        public Controller()
        {
            Data = Array.Empty<T>();
            Count = 0;
        }
        
        #if !NETSTANDARD
        [MemberNotNull(nameof(Data))]
        #endif
        private void Allocate(int size)
        {
            if (Data != null)
            {
                ArrayPool<T>.Shared.Return(Data, _containsReference);

                var old = Data;
                Data = ArrayPool<T>.Shared.Rent(size);

                Array.Copy(old, Data, Math.Min(size, old.Length));
            }
            else
            {
                Data = ArrayPool<T>.Shared.Rent(size);
            }
        }

#if !NETSTANDARD
        [MemberNotNull(nameof(Data))]
#endif
        public void Resize(int newSize)
        {
            var length = Data?.Length ?? 0;
            if (newSize == 0)
            {
                Data = Array.Empty<T>();
                return;
            }

            if (newSize < length)
                return;

            var target = length == 0 ? 16 : length * 2;
            if (target < newSize)
                target = newSize;

            Allocate(target);
        }
    }
}