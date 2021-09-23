using System;
using System.Runtime.CompilerServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs.Boards;
using GameHost.Simulation.TabEcs.Boards.ComponentBoard;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs.HLAPI
{
    public ref struct ComponentBufferAccessor<T>
        where T : struct, IComponentBuffer
    {
        public readonly Span<PooledList<byte>> Source;
        public readonly Span<EntityBoardContainer.ComponentMetadata> Links;

#if DEBUG
        private readonly GameWorld gameWorld;
        private readonly ComponentType ct;
#endif

        public ComponentBufferAccessor(GameWorld gameWorld)
        {
            var componentType = gameWorld.AsComponentType<T>();
            Source = ((BufferComponentBoard) gameWorld
                .Boards
                .ComponentType
                .ComponentBoardColumns[(int) componentType.Id]).AsSpan();
            Links = gameWorld.Boards.Entity.GetComponentColumn(componentType.Id);

#if DEBUG
            this.gameWorld = gameWorld;
            ct = componentType;
#endif
        }


        public ComponentBuffer<T> this[GameEntityHandle gameEntity]
        {
#if DEBUG
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (!gameWorld.Contains(gameEntity))
                    throw new InvalidOperationException($"<{typeof(T).Name}> {gameEntity} does not exist.");
                if (!gameWorld.HasComponent(gameEntity, ct))
                {
                    var msg =
                        $"{gameWorld.Safe(gameEntity)} has no {gameWorld.Boards.ComponentType.NameColumns[(int) ct.Id]}. Existing:\n";
                    var componentList =
                        gameWorld.Boards.Archetype.GetComponentTypes(gameWorld.GetArchetype(gameEntity).Id);
                    foreach (var comp in componentList)
                        msg += $"  [{comp}] {gameWorld.Boards.ComponentType.NameColumns[(int) comp]}\n";

                    throw new InvalidOperationException(msg);
                }

                if (Links.Length < gameEntity.Id + 1)
                    throw new IndexOutOfRangeException(
                        $"<{typeof(T).Name}> Links smaller! Length={Links.Length} < Index={gameEntity.Id}");
                if (Source.Length < Links[(int) gameEntity.Id].Assigned + 1)
                    throw new IndexOutOfRangeException(
                        $"<{typeof(T).Name}> Source smaller! Length={Source.Length} < Index={Links[(int) gameEntity.Id].Assigned}");

                return new ComponentBuffer<T>(Source[Links[(int) gameEntity.Id].Assigned]);
            }
#else
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				unchecked
				{
					return new ComponentBuffer<T>(Source[Links[(int) gameEntity.Id].Assigned]);
				}
			}
#endif
        }
    }
}