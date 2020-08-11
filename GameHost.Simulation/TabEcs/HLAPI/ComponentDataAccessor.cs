using System;
using System.Runtime.CompilerServices;
using GameHost.Simulation.TabEcs.Interfaces;

namespace GameHost.Simulation.TabEcs.HLAPI
{
	public ref struct ComponentDataAccessor<T>
		where T : struct, IComponentData
	{
		public readonly Span<T>                                      Source;
		public readonly Span<EntityBoardContainer.ComponentMetadata> Links;

		public ComponentDataAccessor(GameWorld gameWorld)
		{
			var componentType = gameWorld.AsComponentType<T>();
			Source = ((SingleComponentBoard) gameWorld
			                                 .Boards
			                                 .ComponentType
			                                 .ComponentBoardColumns[(int) componentType.Id]).AsSpan<T>();
			Links = gameWorld.Boards.Entity.GetComponentColumn(componentType.Id);
		}


		public ref T this[GameEntity gameEntity]
		{
#if DEBUG
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ref Source[Links[(int) gameEntity.Id].Assigned];
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