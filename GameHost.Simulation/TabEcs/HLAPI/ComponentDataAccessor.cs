using System;
using System.Runtime.CompilerServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Boards;
using GameHost.Simulation.TabEcs.Boards.ComponentBoard;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs.HLAPI
{
    public ref struct ComponentDataAccessor<T>
        where T : struct
    {
        public readonly Span<T> Source;
        public readonly Span<EntityBoardContainer.ComponentMetadata> Links;

        public ComponentDataAccessor(GameWorld gameWorld, ComponentType original = default)
        {
            var componentType = original.Id == 0 ? gameWorld.AsComponentType(typeof(T)) : original;
            Source = ((SingleComponentBoard) gameWorld
                .Boards
                .ComponentType
                .ComponentBoardColumns[(int) componentType.Id]).AsSpan<T>();
            Links = gameWorld.Boards.Entity.GetComponentColumn(componentType.Id);
        }


        public ref T this[GameEntityHandle gameEntity]
        {
#if DEBUG
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (Links.Length < gameEntity.Id + 1)
                    throw new IndexOutOfRangeException(
                        $"<{typeof(T).Name}> Links smaller! Length={Links.Length} < Index={gameEntity.Id}");
                if (Source.Length < Links[(int) gameEntity.Id].Assigned + 1)
                    throw new IndexOutOfRangeException(
                        $"<{typeof(T).Name}> Source smaller! Length={Source.Length} < Index={Links[(int) gameEntity.Id].Assigned}");

                return ref Source[Links[(int) gameEntity.Id].Assigned];
            }
#else
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				unchecked
				{
					return ref Source[Links[(int) gameEntity.Id].Assigned];
				}
			}
#endif
        }
    }
}