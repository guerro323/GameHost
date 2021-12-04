using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace revghost.Shared.Collections;

public partial struct ValueList<T> : IList<T>, IDisposable
{
    private static bool _containsReference = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    private T[]? _array = Array.Empty<T>();
    private int _count;

    public Span<T> Span => _array.AsSpan(0, _count);

    public ValueList(int capacity)
    {
        _array = ArrayPool<T>.Shared.Rent(capacity);
        _count = 0;
    }

    public ValueList(ReadOnlySpan<T> span)
    {
        _array = null;
        _count = 0;

        AddRange(span);
    }

    public ValueList(IEnumerable<T> enumerable)
    {
        _array = null;
        _count = 0;
        
        AddRange(enumerable);
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index < 0)
            return false;

        RemoveAt(index);
        return true;
    }

    public int Count
    {
        get => _count;
        set => _count = value;
    }

    bool ICollection<T>.IsReadOnly => false;

    public void Add(T item)
    {
        var count = _count;

        _count += 1;
        Resize(_count);

        _array[count] = item;
    }

    public void AddRange(ReadOnlySpan<T> span)
    {
        var count = _count;

        _count += span.Length;
        Resize(_count);

        span.CopyTo(Span[count..]);
    }

    public void AddRange<TEnumerable>(TEnumerable enumerable)
        where TEnumerable : IEnumerable<T>
    {
        if (enumerable is T[] array)
        {
            AddRange(array.AsSpan());
            return;
        }

        if (enumerable is IList<T> list)
        {
            for (var i = 0; i < list.Count; i++)
                Add(list[i]);
            return;
        }

        foreach (var value in enumerable)
            Add(value);
    }

    public void Clear()
    {
        _count = 0;
        if (_containsReference && _array != null)
        {
            Span.Clear();
        }
    }

    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        _array?.CopyTo(array, arrayIndex);
    }

    public Span<T>.Enumerator GetEnumerator() => Span.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        throw new NotImplementedException();
    }

    public int IndexOf(T item)
    {
        if (_count == 0)
            return -1;

        return Array.IndexOf(_array!, item, 0, _count);
    }

    public void Insert(int index, T item)
    {
        if (index >= _count)
        {
            _count = index + 1;
            Resize(_count);

            _array[index] = item;
        }
        else
        {
            Array.Copy(_array!, index, _array!, index + 1, _count - index);
            _array![index] = item;
        }
    }

    public void RemoveAt(int index)
    {
        if (index > _count)
            throw new ArgumentOutOfRangeException();

        _count -= 1;
        if (index < _count)
        {
            Array.Copy(_array!, index + 1, _array!, index, _count - index);
        }

        if (_containsReference)
        {
            _array![index] = default!;
        }
    }

    public T this[int index]
    {
        get
        {
            if (_count == 0)
                throw new IndexOutOfRangeException();

            return _array![index];
        }
        set
        {
            if (index >= _count)
                throw new IndexOutOfRangeException();

            _array![index] = value;
        }
    }

    public void Dispose()
    {
        if (_array != null)
        {
            ArrayPool<T>.Shared.Return(_array, _containsReference);
        }
    }
}