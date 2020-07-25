using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Interfaces;

namespace GameHost.Simulation.TabEcs
{
	public partial struct ComponentBuffer<T> : IList<T>
		where T : struct
	{
		private PooledList<byte> backing;

		public ComponentBuffer(PooledList<byte> backing)
		{
			this.backing = backing;
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
			if (Unsafe.SizeOf<TToReinterpret>() != Unsafe.SizeOf<T>())
				throw new InvalidOperationException("Invalid size");
			return new ComponentBuffer<TToReinterpret>(backing);
		}

		public Span<T> Span => MemoryMarshal.Cast<byte, T>(backing.Span);
	}
}