﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Collections.Pooled;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Simulation.TabEcs
{
	public partial struct ComponentBuffer<T> : IList<T>, IReadOnlyList<T>
		where T : struct
	{
		public bool IsCreated => backing != null;
		
		private PooledList<byte> backing;

		public ComponentBuffer(PooledList<byte> backing)
		{
			this.backing = backing;
		}

		/// <summary>
		/// Clean all items that are default/zero
		/// </summary>
		public void ClearZeroes()
		{
			for (var i = 0; i < Count; i++)
			{
				if (!UnsafeUtility.SameData(this[i], default))
					continue;

				RemoveAt(i);
				i--;
			}
		}

		public void Add(T value)
		{
			backing.AddRange(MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1)));
		}

		public void AddReinterpret<TToReinterpret>(TToReinterpret value)
		{
			Add(Unsafe.As<TToReinterpret, T>(ref value));
		}
		
		public void AddRange(Span<T> span)
		{
			backing.AddRange(MemoryMarshal.AsBytes(span));
		}

		public void AddRangeReinterpret<TToReinterpret>(Span<TToReinterpret> span) 
			where TToReinterpret : struct
		{
			AddRange(MemoryMarshal.Cast<TToReinterpret, T>(span));
		}

		public ComponentBuffer<TToReinterpret> Reinterpret<TToReinterpret>()
			where TToReinterpret : struct
		{
#if DEBUG
			if (Unsafe.SizeOf<TToReinterpret>() != Unsafe.SizeOf<T>())
				throw new InvalidOperationException("Invalid size");
#endif
			return new ComponentBuffer<TToReinterpret>(backing);
		}

		public delegate ref readonly T1 PredicateDelegate<T1>(ref T t);
		
		// a Contains predicate that does not allocate at all!
		public bool Contains<T1>(PredicateDelegate<T1> variable, T1 wanted)
		{
			var span = Span;
			if (span.IsEmpty)
			{
				return false;
			}

			var d      = default(T);
			// What happens:
			// . We first get the read-only reference to the target variable from 'd'.
			// . We then cast that variable to the right type `T1.
			// . We then get the offset between 'd' and that variable.
			//
			// We make the delegate return a 'ref readonly' because of readonly structs.
			var offset = Unsafe.ByteOffset(ref d, ref Unsafe.As<T1, T>(ref Unsafe.AsRef(in variable(ref d))));
			foreach (ref var element in span)
			{
				if (Unsafe.AddByteOffset(ref Unsafe.As<T, T1>(ref element), offset).Equals(wanted))
					return true;
			}

			return false;
		}

		public Span<T> Span => MemoryMarshal.Cast<byte, T>(backing.Span);
	}
}