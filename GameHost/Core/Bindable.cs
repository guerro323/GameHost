using System;
using System.Collections.Generic;

namespace GameHost.Core
{
    public delegate void ValueChanged<in T>(T previous, T next);

    public abstract class BindableListener : IDisposable
    {
        public abstract void Dispose();
    }

    public class BindableListener<T> : BindableListener
    {
        private readonly ValueChanged<T> valueChanged;
        private readonly WeakReference   bindableReference;

        public BindableListener(WeakReference bindableReference, ValueChanged<T> valueChanged)
        {
            this.valueChanged      = valueChanged;
            this.bindableReference = bindableReference;
        }

        public override void Dispose()
        {
            if (bindableReference.IsAlive)
            {
                ((Bindable<T>) bindableReference.Target).Unsubscribe(valueChanged);
            }
        }
    }

    public class Bindable<T> : IDisposable
    {
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
            get => defaultValue;
            // should we also do a subscription format when default get changed?
            set => defaultValue = value;
        }

        private T value;
        private T defaultValue;

        protected virtual IEnumerable<ValueChanged<T>> SubscribedListeners { get; set; } = new List<ValueChanged<T>>();

        public Bindable(T defaultValue = default, T initialValue = default)
        {
            this.defaultValue = defaultValue;
            if (EqualityComparer<T>.Default.Equals(initialValue, default) && !EqualityComparer<T>.Default.Equals(defaultValue, default))
                this.value = defaultValue;
            else
                this.value = initialValue;
        }

        public virtual void Dispose()
        {
        }

        private ValueChanged<T> currentListener;

        protected virtual void InvokeOnUpdate(ref T value)
        {
            var currentList = new List<ValueChanged<T>>((List<ValueChanged<T>>) SubscribedListeners);
            foreach (var listener in currentList)
            {
                currentListener = listener;
                listener(this.value, value);
            }

            this.value = value;
        }

        /// <summary>
        /// Unsubcribe the current listener
        /// </summary>
        public void UnsubscribeCurrent()
        {
            if (currentListener != null)
                ((List<ValueChanged<T>>) SubscribedListeners).Remove(currentListener);
        }

        public bool Unsubscribe(ValueChanged<T> listener)
        {
            return ((List<ValueChanged<T>>) SubscribedListeners).Remove(listener);
        }

        public virtual BindableListener Subscribe(in ValueChanged<T> listener, bool invokeNow = false)
        {
            if (SubscribedListeners is List<ValueChanged<T>> list)
            {
                if (!list.Contains(listener))
                    list.Add(listener);
            }
            else
                throw new InvalidOperationException("You've replaced the list type by something else!");

            if (invokeNow)
                listener(value, value);

            return new BindableListener<T>(new WeakReference(this, true), listener);
        }

        public void SetDefault() => Value = Default;
    }

    public readonly struct ReadOnlyBindable<T>
    {
        private readonly Bindable<T> source;

        public T Value   => source.Value;
        public T Default => source.Default;

        public ReadOnlyBindable(Bindable<T> source)
        {
            this.source = source;
        }

        public void UnsubscribeCurrent() => source.UnsubscribeCurrent();

        public bool Unsubscribe(ValueChanged<T> listener) => source.Unsubscribe(listener);

        public BindableListener Subscribe(in ValueChanged<T> listener, bool invokeNow = false) => source.Subscribe(listener, invokeNow);

        public static implicit operator ReadOnlyBindable<T>(Bindable<T> origin)
        {
            return new(origin);
        }
    }
}