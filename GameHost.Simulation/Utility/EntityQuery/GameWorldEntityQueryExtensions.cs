using System;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.Utility.EntityQuery
{
    public static class GameWorldEntityQueryExtensions
    {
        public static ArchetypeEnumerator QueryArchetype(this GameWorld world, Span<ComponentType> all,
            Span<ComponentType> none)
        {
            return QueryArchetype(world, new FinalizedQuery {All = all, None = none});
        }

        public static ArchetypeEnumerator QueryArchetype(this GameWorld world, FinalizedQuery finalizedQuery)
        {
            return new ArchetypeEnumerator
            {
                Board = world.Boards.Archetype, Archetypes = world.Boards.Archetype.Registered,
                finalizedQuery = finalizedQuery
            };
        }

        public static EntityEnumerator QueryEntityWith(this GameWorld world, Span<ComponentType> all)
        {
            return QueryEntity(world, new FinalizedQuery {All = all, None = Span<ComponentType>.Empty});
        }

        public static EntityEnumerator QueryEntityWithout(this GameWorld world, Span<ComponentType> none)
        {
            return QueryEntity(world, new FinalizedQuery {All = Span<ComponentType>.Empty, None = none});
        }

        public static EntityEnumerator QueryEntity(this GameWorld world, Span<ComponentType> all,
            Span<ComponentType> none)
        {
            return QueryEntity(world, new FinalizedQuery {All = all, None = none});
        }

        public static EntityEnumerator QueryEntity(this GameWorld world, FinalizedQuery finalizedQuery)
        {
            return new EntityEnumerator {World = world, Inner = QueryArchetype(world, finalizedQuery)};
        }

        public static bool TryGetSingleton<T>(this GameWorld world, out T singleton) where T : struct, IComponentData
        {
            var enumerator = QueryEntityWith(world, stackalloc[] {world.AsComponentType<T>()});
            if (!enumerator.TryGetFirst(out var entity))
            {
                singleton = default;
                return false;
            }

            singleton = world.GetComponentData<T>(entity);
            return true;
        }

        public static bool TryGetSingleton<T>(this GameWorld world, out GameEntityHandle entityHandle)
            where T : struct, IComponentData
        {
            var enumerator = QueryEntityWith(world, stackalloc[] {world.AsComponentType<T>()});
            return enumerator.TryGetFirst(out entityHandle);
        }
    }
}