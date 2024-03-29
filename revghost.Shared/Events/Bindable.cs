namespace revghost.Shared.Events;

public delegate void ValueChanged<in T>(T previous, T next);

public abstract class BindableListener : IDisposable
{
    public abstract void Dispose();
}

public class BindableListener<T> : BindableListener
{
    private readonly WeakReference bindableReference;
    private readonly ValueChanged<T> valueChanged;

    public BindableListener(WeakReference bindableReference, ValueChanged<T> valueChanged)
    {
        this.valueChanged = valueChanged;
        this.bindableReference = bindableReference;
    }

    public override void Dispose()
    {
        if (bindableReference.IsAlive) ((Bindable<T>) bindableReference.Target).Unsubscribe(valueChanged);
    }
}

public class Bindable<T> : IDisposable
{
    private ValueChanged<T>? currentListener;

    private T value;

    public Bindable(T defaultValue = default, T initialValue = default)
    {
        this.Default = defaultValue;
        if (EqualityComparer<T>.Default.Equals(initialValue, default) &&
            !EqualityComparer<T>.Default.Equals(defaultValue, default))
            value = defaultValue;
        else
            value = initialValue;
    }

    public T Value
    {
        get => value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(this.value, value))
                return;
            InvokeOnUpdate(ref value);
        }
    }

    public T Default
    {
        get;
        // should we also do a subscription format when default get changed?
        set;
    }

    protected virtual List<ValueChanged<T>> SubscribedListeners { get; set; } = new();

    public virtual void Dispose()
    {
    }

    protected virtual void InvokeOnUpdate(ref T value)
    {
        int count;
        DisposableArray<ValueChanged<T>> disposable;
        ValueChanged<T>[] array;
        lock (SubscribedListeners)
        {
            count = SubscribedListeners.Count;

            disposable = DisposableArray<ValueChanged<T>>.Rent(count, out array);
            SubscribedListeners.CopyTo(array);
        }

        using (disposable)
        {
            foreach (var listener in array.AsSpan(0, count))
            {
                currentListener = listener;
                listener(this.value, value);
            }

            this.value = value;
        }
    }

    /// <summary>
    ///     Unsubcribe the current listener
    /// </summary>
    public void UnsubscribeCurrent()
    {
        lock (SubscribedListeners)
        {
            if (currentListener != null)
                SubscribedListeners.Remove(currentListener);
        }
    }

    public bool Unsubscribe(ValueChanged<T> listener)
    {
        lock (SubscribedListeners)
        {
            return SubscribedListeners.Remove(listener);
        }
    }

    public virtual BindableListener Subscribe(in ValueChanged<T> listener, bool invokeNow = false)
    {
        lock (SubscribedListeners)
        {
            if (!SubscribedListeners.Contains(listener))
                SubscribedListeners.Add(listener);
        }

        if (invokeNow)
            listener(Default, value);

        return new BindableListener<T>(new WeakReference(this, true), listener);
    }

    public void SetDefault()
    {
        Value = Default;
    }
}

public readonly struct ReadOnlyBindable<T>
{
    private readonly Bindable<T> source;

    public T Value => source.Value;
    public T Default => source.Default;

    public ReadOnlyBindable(Bindable<T> source)
    {
        this.source = source;
    }

    public void UnsubscribeCurrent()
    {
        source.UnsubscribeCurrent();
    }

    public bool Unsubscribe(ValueChanged<T> listener)
    {
        return source.Unsubscribe(listener);
    }

    public BindableListener Subscribe(in ValueChanged<T> listener, bool invokeNow = false)
    {
        return source.Subscribe(listener, invokeNow);
    }

    public static implicit operator ReadOnlyBindable<T>(Bindable<T> origin)
    {
        return new ReadOnlyBindable<T>(origin);
    }
}

public class ManagedReadOnlyBindable<T>
{
    private readonly Bindable<T> source;

    public ManagedReadOnlyBindable(Bindable<T> source)
    {
        this.source = source;
    }

    public T Value => source.Value;
    public T Default => source.Default;

    public void UnsubscribeCurrent()
    {
        source.UnsubscribeCurrent();
    }

    public bool Unsubscribe(ValueChanged<T> listener)
    {
        return source.Unsubscribe(listener);
    }

    public BindableListener Subscribe(in ValueChanged<T> listener, bool invokeNow = false)
    {
        return source.Subscribe(listener, invokeNow);
    }

    public static implicit operator ManagedReadOnlyBindable<T>(Bindable<T> origin)
    {
        return new ManagedReadOnlyBindable<T>(origin);
    }
}