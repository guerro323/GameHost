using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Interfaces;

namespace GameHost.Simulation.TabEcs
{
	public struct ComponentBuffer<T>
		where T : struct, IComponentBuffer
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

		public Span<T> Span => MemoryMarshal.Cast<byte, T>(backing.Span);
	}
}