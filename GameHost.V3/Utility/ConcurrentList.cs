using System.Collections;
using System.Collections.Generic;
using GameHost.V3.Threading.V2;

namespace GameHost.V3.Utility
{
    public class ConcurrentList<T> : IList<T>
    {
        private readonly List<T> _backing = new();
        private readonly SynchronizationManager _synchronization = new();

        public IEnumerator<T> GetEnumerator()
        {
            return _backing.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _backing.GetEnumerator();
        }

        public void Add(T item)
        {
            using (_synchronization.Synchronize())
            {
                _backing.Add(item);
            }
        }

        public void Clear()
        {
            using (_synchronization.Synchronize())
                _backing.Clear();
        }

        public bool Contains(T item)
        {
            using (_synchronization.Synchronize())
                return _backing.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            using (_synchronization.Synchronize())
                _backing.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            using (_synchronization.Synchronize())
                return _backing.Remove(item);
        }

        public int Count
        {
            get
            {
                using (_synchronization.Synchronize())
                    return _backing.Count;
            }
        }

        public bool IsReadOnly => false;

        public int IndexOf(T item)
        {
            using (_synchronization.Synchronize())
                return _backing.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            using (_synchronization.Synchronize())
                _backing.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            using (_synchronization.Synchronize())
                _backing.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                using (_synchronization.Synchronize())
                    return _backing[index];
            }
            set
            {
                using (_synchronization.Synchronize())
                    _backing[index] = value;
            }
        }
    }
}