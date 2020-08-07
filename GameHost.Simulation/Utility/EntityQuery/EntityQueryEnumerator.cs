using System;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public ref struct EntityQueryEnumerator
	{
		public ArchetypeBoardContainer Board;
		public Span<uint>.Enumerator              Inner;

		public GameEntity Current { get; private set; }

		private int  index;
		private bool canMove;

		private bool MoveInnerNext()
		{
			index   = 0;
			canMove = Inner.MoveNext();

			return canMove;
		}

		public bool MoveNext()
		{
			while (true)
			{
				if (!canMove && !MoveInnerNext()) 
					return false;

				var entitySpan = Board.GetEntities(Inner.Current);
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