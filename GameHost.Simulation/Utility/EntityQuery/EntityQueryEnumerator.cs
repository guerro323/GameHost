using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public unsafe struct EntityQueryEnumerator
	{
		public ArchetypeBoardContainer Board;
		public uint*                   Inner;
		public int                    InnerIndex;
		public int                    InnerSize;

		public bool* Swapback;

		public GameEntity Current { get; private set; }

		private int  index;
		private bool canMove;

		private bool MoveInnerNext()
		{
			index = 0;

			InnerIndex++;
			canMove = InnerIndex < InnerSize;

			return canMove;
		}

		public bool MoveNext()
		{
			while (true)
			{
				ref var swap = ref Unsafe.AsRef<bool>(Swapback);
				if (swap) 
				{
					index--;
					swap = false;
				}

				if (!canMove && !MoveInnerNext())
					return false;

				var entitySpan = Board.GetEntities(Inner[InnerIndex]);
				if (entitySpan.Length <= index)
				{
					canMove = false;
					continue;
				}

				Current = new GameEntity(entitySpan[index++]);
				return true;
			}
		}

		public EntityQueryEnumerator GetEnumerator() => this;

		public GameEntity First
		{
			get
			{
				while (MoveNext())
					return Current;
				return default;
			}
		}
	}
}