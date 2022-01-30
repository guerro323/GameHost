using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace revghost.Shared.Collections;

public partial struct ValueList<T>
{
    [MemberNotNull(nameof(_array))]
    private void Allocate(int size)
    {
        if (_array != null)
        {
            ArrayPool<T>.Shared.Return(_array, _containsReference);

            var old = _array;
            _array = ArrayPool<T>.Shared.Rent(size);

            Array.Copy(old, _array, old.Length);
        }
        else {
            _array = ArrayPool<T>.Shared.Rent(size);
        }
    }

    [MemberNotNull(nameof(_array))]
    private void Resize(int newSize)
    {
        var length = _array?.Length ?? 0;
        if (newSize == 0)
        {
            _array = Array.Empty<T>();
            return;
        }

        var target = length == 0 ? 16 : length * 2;
        if (target < newSize)
            target = newSize;

        Allocate(target);
    }
}