using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace revghost.Shared.Collections;

public partial struct ValueList<T> : IList<T>, IDisposable
{
    private static bool _containsReference = RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public Span<T> Span => _controller.Data.AsSpan(0, _controller.Count);

    private Controller[] _controllerBacking = ArrayPool<Controller>.Shared.Rent(1);
    private ref Controller _controller
    {
        get
        {
            if (_controllerBacking == null || _controllerBacking.Length == 0)
                throw new InvalidOperationException("ValueList wasn't initialized correctly");
            return ref _controllerBacking[0];
        }
    }

    public ValueList(int capacity) : this()
    {
        _controller.Data = ArrayPool<T>.Shared.Rent(capacity);
    }

    public ValueList(ReadOnlySpan<T> span) : this()
    {
        AddRange(span);
    }

    public ValueList(IEnumerable<T> enumerable) : this()
    {
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
        get => _controller.Count;
        set => _controller.Count = value;
    }

    bool ICollection<T>.IsReadOnly => false;

    public void Add(T item)
    {
        ref var controller = ref _controller;
        
        var count = controller.Count;

        controller.Count += 1;
        controller.Resize(controller.Count);
        controller.Data[count] = item;
    }

    public void AddRange(ReadOnlySpan<T> span)
    {
        ref var controller = ref _controller;

        var count = controller.Count;

        controller.Count += span.Length;
        controller.Resize(controller.Count);

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
        _controller.Count = 0;
        if (_containsReference && _controller.Data != null)
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
        _controller.Data?.CopyTo(array, arrayIndex);
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
        if (_controller.Count == 0)
            return -1;

        return Array.IndexOf(_controller.Data!, item, 0, _controller.Count);
    }

    public void Insert(int index, T item)
    {
        if (index >= _controller.Count)
        {
            _controller.Count = index + 1;
            _controller.Resize(_controller.Count);

            _controller.Data[index] = item;
        }
        else
        {
            Array.Copy(_controller.Data!, index, _controller.Data!, index + 1, _controller.Count - index);
            _controller.Data![index] = item;
        }
    }

    public void RemoveAt(int index)
    {
        if (index > _controller.Count)
            throw new ArgumentOutOfRangeException();

        _controller.Count -= 1;
        if (index < _controller.Count)
        {
            Array.Copy(_controller.Data!, index + 1, _controller.Data!, index, _controller.Count - index);
        }

        if (_containsReference)
        {
            _controller.Data![index] = default!;
        }
    }

    public T this[int index]
    {
        get
        {
            if (_controller.Count == 0)
                throw new IndexOutOfRangeException();

            return _controller.Data![index];
        }
        set
        {
            if (index >= _controller.Count)
                throw new IndexOutOfRangeException();

            _controller.Data![index] = value;
        }
    }

    public void Return()
    {
        if (_controller.Data != null)
        {
            ArrayPool<T>.Shared.Return(_controller.Data, _containsReference);
        }
    }
    
    public void Dispose()
    {
        if (_controller.Data != null)
        {
            ArrayPool<T>.Shared.Return(_controller.Data, _containsReference);
        }

        ArrayPool<Controller>.Shared.Return(_controllerBacking, true);
    }
}