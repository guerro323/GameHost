using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GameHost.Core.Bindables
{
    public delegate void ValueChanged<in T>(T previous, T next);
    
    public class Bindable<T> : IDisposable
    {
        public T Value
        {
            get => value;
            set
            {
                if (!protection.CanModifyValue)
                    throw new InvalidOperationException("Can not modify values");

                if (EqualityComparer<T>.Default.Equals(this.value, value))
                    return;
                Console.WriteLine("on update! " + typeof(T));
                InvokeOnUpdate(ref value);
            }
        }

        public T Default
        {
            get => defaultValue;
            // should we also do a subscription format when default get changed?
            set
            {
                if (!protection.CanModifyValue)
                    throw new InvalidOperationException("Can not modify values");
                    
                defaultValue = value;
            }
        }

        private ProtectedValue protection;
        private T value;
        private T defaultValue;

        protected virtual IEnumerable<ValueChanged<T>> SubscribedListeners { get; set; } = new List<ValueChanged<T>>();

        public Bindable(T defaultValue = default, T initialValue = default, object protection = null)
        {
            this.defaultValue = defaultValue;
            if (EqualityComparer<T>.Default.Equals(initialValue, default) && !EqualityComparer<T>.Default.Equals(defaultValue, default))
                this.value = defaultValue;
            else
                this.value = initialValue;

            this.protection = new ProtectedValue(protection);
        }

        public virtual void Dispose()
        {
        }

        protected virtual void InvokeOnUpdate(ref T value)
        {
            foreach (var listener in (List<ValueChanged<T>>)SubscribedListeners)
            {
                listener(this.value, value);
            }

            this.value = value;
        }

        public virtual void Subscribe(in ValueChanged<T> listener, bool invokeNow = false)
        {
            if (SubscribedListeners is List<ValueChanged<T>> list && !list.Contains(listener))
                list.Add(listener);
            else
                throw new InvalidOperationException("You've replaced the list type by something else!");

            if (invokeNow)
                listener(value, value);
        }

        public void SetDefault() => Value = Default;

        public void EnableProtection(bool state, object protectedObject)
        {
            if (!protection.SetEnabled(protectedObject, state))
                throw new InvalidOperationException("Invalid protection object");
        }

        private struct ProtectedValue
        {
            private object Protection;
            private bool   IsEnabled;

            public bool CanModifyValue => Protection == null || (Protection != null && !IsEnabled);

            public ProtectedValue(object protection)
            {
                Protection = protection;
                IsEnabled  = true;
            }

            public bool SetEnabled(object protection, bool state)
            {
                if (Protection != null && Protection != protection)
                    return false;

                IsEnabled = state;
                return true;
            }
        }
    }
}
