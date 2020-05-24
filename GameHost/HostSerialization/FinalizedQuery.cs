using System;
using System.Diagnostics.Contracts;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public ref struct FinalizedQuery
    {
        public Span<Type> All;
        public Span<Type> None;
    }

    public struct Query
    {
        public Type[] All, None;

        [Pure]
        public FinalizedQuery Final => new FinalizedQuery {All = All, None = None};
        
        public static implicit operator FinalizedQuery (Query query)
        {
            return query.Final;
        }
    }

    public static class QueryExtensions
    {
        public ref struct ChunkEnumerator
        {
            public Span<RevolutionChunk> Chunks;
            public FinalizedQuery        finalizedQuery;

            public RevolutionChunk Current { get; set; }

            private int index;

            public bool MoveNext()
            {
                while (Chunks.Length > index)
                {
                    var chunk = Chunks[index++];

                    var span = chunk.Span;
                    if (span.Length == 0)
                        continue;

                    var matches = 0;
                    for (var i = 0; i != finalizedQuery.All.Length; i++)
                    {
                        if (chunk.Components.ContainsKey(finalizedQuery.All[i]))
                            matches++;
                    }

                    if (matches != finalizedQuery.All.Length)
                        continue;

                    matches = 0;
                    for (var i = 0; i != finalizedQuery.None.Length && matches == 0; i++)
                    {
                        if (chunk.Components.ContainsKey(finalizedQuery.None[i]))
                            matches++;
                    }

                    if (matches > 0)
                        continue;

                    Current = chunk;
                    return true;
                }

                return false;
            }

            public ChunkEnumerator GetEnumerator() => this;
        }

        public static ChunkEnumerator QueryChunks(this RevolutionWorld world, Span<Type> all, Span<Type> none)
        {
            return QueryChunks(world, new FinalizedQuery {All = all, None = none});
        }

        public static ChunkEnumerator QueryChunks(this RevolutionWorld world, FinalizedQuery finalizedQuery)
        {
            return new ChunkEnumerator {Chunks = world.Chunks, finalizedQuery = finalizedQuery};
        }
        
        public ref struct EntityRawEnumerator
        {
            public ChunkEnumerator Inner;

            public ref readonly RawEntity Current => ref Inner.Current.Span[index - 1];
            
            private int index;

            public bool MoveNext()
            {
                if (Inner.Current == null)
                {
                    if (!Inner.MoveNext())
                        return false;
                }

                var entities = Inner.Current.Span;
                while (entities.Length > index)
                {
                    index++;
                    return true;
                }

                if (!Inner.MoveNext())
                    return false;
                return true;
            }

            public EntityRawEnumerator GetEnumerator() => this;

            public RawEntity First
            {
                get
                {
                    while (MoveNext())
                        return Current;
                    return default;
                }
            }
            
            public bool TryGetFirst(out RawEntity entity)
            {
                if (!First.Equals(default))
                {
                    entity = Current;
                    return true;
                }

                entity = default;
                return false;
            }
        }
        
        public ref struct EntityEnumerator
        {
            public ChunkEnumerator Inner;
            public RevolutionWorld World;

            public RevolutionEntity Current => new RevolutionEntity(World.Accessor, Inner.Current.Span[index - 1]);
            
            private int index;

            public bool MoveNext()
            {
                if (Inner.Current == null)
                {
                    if (!Inner.MoveNext())
                        return false;
                }

                var entities = Inner.Current.Span;
                while (entities.Length > index)
                {
                    index++;
                    return true;
                }

                if (!Inner.MoveNext())
                    return false;
                return true;
            }

            public EntityEnumerator GetEnumerator() => this;

            public RevolutionEntity First
            {
                get
                {
                    while (MoveNext())
                        return Current;
                    return default;
                }
            }

            public bool TryGetFirst(out RevolutionEntity entity)
            {
                if (First.IsAlive)
                {
                    entity = Current;
                    return true;
                }

                entity = default;
                return false;
            }
        }

        public static EntityEnumerator QueryEntities(this RevolutionWorld world, Span<Type> all, Span<Type> none)
        {
            return QueryEntities(world, new FinalizedQuery {All = all, None = none});
        }

        public static EntityEnumerator QueryEntities(this RevolutionWorld world, FinalizedQuery finalizedQuery)
        {
            return new EntityEnumerator {World = world, Inner = QueryChunks(world, finalizedQuery)};
        }
        
        public static EntityRawEnumerator QueryRawEntities(this RevolutionWorld world, Span<Type> all, Span<Type> none)
        {
            return QueryRawEntities(world, new FinalizedQuery {All = all, None = none});
        }

        public static EntityRawEnumerator QueryRawEntities(this RevolutionWorld world, FinalizedQuery finalizedQuery)
        {
            return new EntityRawEnumerator {Inner = QueryChunks(world, finalizedQuery)};
        }
    }
}
