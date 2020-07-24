using System;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public static class GameWorldEntityQueryExtensions
	{
		public static ArchetypeEnumerator QueryArchetype(this GameWorld world, Span<ComponentType> all, Span<ComponentType> none)
		{
			return QueryArchetype(world, new FinalizedQuery {All = all, None = none});
		}

		public static ArchetypeEnumerator QueryArchetype(this GameWorld world, FinalizedQuery finalizedQuery)
		{
			return new ArchetypeEnumerator {Board = world.Boards.Archetype, Archetypes = world.Boards.Archetype.Registered, finalizedQuery = finalizedQuery};
		}
		
		public static EntityEnumerator QueryEntityWith(this GameWorld world, Span<ComponentType> all)
		{
			return QueryEntity(world, new FinalizedQuery {All = all, None = Span<ComponentType>.Empty});
		}
		
		public static EntityEnumerator QueryEntityWithout(this GameWorld world, Span<ComponentType> none)
		{
			return QueryEntity(world, new FinalizedQuery {All = Span<ComponentType>.Empty, None = none});
		}

		public static EntityEnumerator QueryEntity(this GameWorld world, Span<ComponentType> all, Span<ComponentType> none)
		{
			return QueryEntity(world, new FinalizedQuery {All = all, None = none});
		}

		public static EntityEnumerator QueryEntity(this GameWorld world, FinalizedQuery finalizedQuery)
		{
			return new EntityEnumerator {World = world, Inner = QueryArchetype(world, finalizedQuery)};
		}
	}
}