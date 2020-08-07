using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public ref struct EntityEnumerator
	{
		public ArchetypeEnumerator Inner;
		public GameWorld World;

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
				if (!canMove && !MoveInnerNext()) return false;

				var entitySpan = Inner.Board.GetEntities(Inner.Current.Id);
				if (entitySpan.Length <= index)
				{
					canMove = false;
					continue;
				}

				Current = new GameEntity(entitySpan[index++]);
				return true;
			}
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