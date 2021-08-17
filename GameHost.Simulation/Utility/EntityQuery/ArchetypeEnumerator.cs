using System;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public ref struct ArchetypeEnumerator
	{
		public Span<EntityArchetype>   Archetypes;
		public ArchetypeBoardContainer Board;

		public FinalizedQuery finalizedQuery;

		public EntityArchetype Current { get; set; }

		private int index;

		public bool MoveNext()
		{
			while (Archetypes.Length > index)
			{
				var arch = Archetypes[index++];

				var span = Board.GetEntities(arch.Id);
				if (span.Length == 0)
					continue;

				var matches       = 0;
				var componentSpan = Board.GetComponentTypes(arch.Id);
				for (var i = 0; i != finalizedQuery.All.Length; i++)
				{
#if NETSTANDARD
					foreach (var element in componentSpan)
						if (element == finalizedQuery.All[i].Id)
						{
							matches++;
							break;
						}
#else
					if (componentSpan.Contains(finalizedQuery.All[i].Id))
						matches++;
#endif
				}

				if (matches != finalizedQuery.All.Length)
					continue;

				matches = 0;
				for (var i = 0; i != finalizedQuery.None.Length && matches == 0; i++)
				{
#if NETSTANDARD
					foreach (var element in componentSpan)
						if (element == finalizedQuery.None[i].Id)
						{
							matches++;
							break;
						}
#else
					if (componentSpan.Contains(finalizedQuery.None[i].Id))
						matches++;
#endif
				}

				if (matches > 0)
					continue;

				Current = arch;
				return true;
			}

			return false;
		}

		public ArchetypeEnumerator GetEnumerator() => this;
	}
}