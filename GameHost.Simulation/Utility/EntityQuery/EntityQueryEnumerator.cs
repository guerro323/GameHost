using System;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public ref struct EntityQueryEnumerator
	{
		public ArchetypeBoardContainer Board;
		public Span<uint>.Enumerator              Inner;

		public GameEntity Current { get; private set; }

		private int index;

		public bool MoveNext()
		{
			while (true)
			{
				if (!Inner.MoveNext()) 
					return false;

				var entitySpan = Board.GetEntities(Inner.Current);
				while (entitySpan.Length > index)
				{
					Current = new GameEntity(entitySpan[index]);
					index++;
					return true;
				}
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