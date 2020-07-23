using System;
using System.Runtime.InteropServices;
using GameHost.Core.Ecs;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.Utility.Resource.Interfaces;

namespace GameHost.Simulation.Utility.Resource.Systems
{
	public abstract class KeepAliveResourceFromData<TResource, TData> : KeepAliveResourceSystemBase
		where TResource : IGameResourceDescription
		where TData : struct, IComponentData
	{
		public override Type ResourceType => typeof(TResource);

		private GameWorld gameWorld;

		protected KeepAliveResourceFromData(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref gameWorld);
		}

		protected internal override void KeepAlive(Span<bool> keep, Span<GameEntity> resources)
		{
			var componentType = gameWorld.GetComponentType<TData>();
			if (!(gameWorld.Boards.ComponentType.ComponentBoardColumns[(int) componentType.Id] is SingleComponentBoard componentBoard))
				return;

			var dataSpan = componentBoard.AsSpan<TData>();
			// It should be of the same length
			KeepAlive(keep, dataSpan, MemoryMarshal.Cast<GameEntity, GameResource<TResource>>(resources));
		}

		protected abstract void KeepAlive(Span<bool> keep, Span<TData> self, Span<GameResource<TResource>> resources);
	}
}