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

		private    GameEntity current;
		public ref GameEntity Current
		{
			get
			{
				fixed (GameEntity* e = &current)
				{
					return ref *e;
				}
			}
		}

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
				// If the user set Current to default, this mean we need to decrease the index
				if (current == default)
					index--;

				if (!canMove && !MoveInnerNext())
					return false;

				var entitySpan = Board.GetEntities(Inner[InnerIndex]);
				if (entitySpan.Length <= index)
				{
					canMove = false;
					continue;
				}

				current = new GameEntity(entitySpan[index++]);
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