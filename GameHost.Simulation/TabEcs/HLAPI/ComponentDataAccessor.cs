using System;
using System.Runtime.CompilerServices;
using Collections.Pooled;
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


		public ref T this[GameEntityHandle gameEntity]
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

	public ref struct ComponentBufferAccessor<T>
		where T : struct, IComponentBuffer
	{
		public readonly Span<PooledList<byte>>                       Source;
		public readonly Span<EntityBoardContainer.ComponentMetadata> Links;

		public ComponentBufferAccessor(GameWorld gameWorld)
		{
			var componentType = gameWorld.AsComponentType<T>();
			Source = ((BufferComponentBoard) gameWorld
			                                 .Boards
			                                 .ComponentType
			                                 .ComponentBoardColumns[(int) componentType.Id]).AsSpan();
			Links = gameWorld.Boards.Entity.GetComponentColumn(componentType.Id);
		}


		public ComponentBuffer<T> this[GameEntityHandle gameEntity]
		{
#if DEBUG
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new ComponentBuffer<T>(Source[Links[(int) gameEntity.Id].Assigned]);
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