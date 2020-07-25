using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Interfaces;
using NetFabric.Hyperlinq;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Simulation.TabEcs
{
	public partial struct ComponentBuffer<T>
		where T : struct
	{
		public Span<T>.Enumerator GetEnumerator()
		{
			return Span.GetEnumerator();
		}
		
		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			// todo: remove array copy
			return (IEnumerator<T>) Span.ToArray().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			// todo: remove array copy
			return (IEnumerator<T>) Span.ToArray().GetEnumerator();
		}

		public void Clear()
		{
			backing.Clear();
		}

		public bool Contains(T item)
		{
			return Span.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			Span.Slice(arrayIndex).CopyTo(array);
		}

		public bool Remove(T item)
		{
			var index = IndexOf(item);
			if (index < 0) 
				return false;
			
			RemoveAt(IndexOf(item));
			return true;

		}

		public int Count => Span.Length;
		public bool IsReadOnly => false;
		public int IndexOf(T item)
		{
			var span = Span;
			for (var i = 0; i != span.Length; i++)
			{
				if (Unsafe.AreSame(ref span[i], ref item))
					return i;
			}

			return -1;
		}

		public void Insert(int index, T item)
		{
			backing.InsertRange(index, MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref item, 1)));
		}

		public void RemoveAt(int index)
		{
			backing.RemoveRange(index, Unsafe.SizeOf<T>());
		}

		public T this[int index]
		{
			get => Span[index];
			set => Span[index] = value;
		}
	}
}