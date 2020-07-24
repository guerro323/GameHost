using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public ref struct EntityEnumerator
	{
		public ArchetypeEnumerator Inner;
		public GameWorld World;

		public GameEntity Current { get; private set; }

		private int index;

		public bool MoveNext()
		{
			if (Inner.Current.Id == 0)
			{
				if (!Inner.MoveNext())
					return false;
			}

			var entitySpan = Inner.Board.GetEntities(Inner.Current.Id);
			while (entitySpan.Length > index)
			{
				Current = new GameEntity(entitySpan[index]);
				index++;
				return true;
			}

			if (!Inner.MoveNext())
				return false;
			return true;
		}

		public EntityEnumerator GetEnumerator() => this;

		public GameEntity First
		{
			get
			{
				while (MoveNext())
					return Current;
				return default;
			}
		}

		public bool TryGetFirst(out GameEntity entity)
		{
			if (World.Contains(First))
			{
				entity = Current;
				return true;
			}

			entity = default;
			return false;
		}
	}
}