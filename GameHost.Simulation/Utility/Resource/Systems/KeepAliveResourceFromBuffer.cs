using System;
using System.Runtime.InteropServices;
using GameHost.Core.Ecs;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.Utility.Resource.Interfaces;

namespace GameHost.Simulation.Utility.Resource.Systems
{
	public abstract class KeepAliveResourceFromBuffer<TResource, TBuffer> : KeepAliveResourceSystemBase
		where TResource : IGameResourceDescription
		where TBuffer : struct, IComponentBuffer
	{
		public override Type ResourceType => typeof(TResource);

		private GameWorld gameWorld;
		
		protected KeepAliveResourceFromBuffer(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref gameWorld);
		}

		protected internal override void KeepAlive(Span<bool> keep, Span<GameEntity> resources)
		{
			var componentType = gameWorld.GetComponentType<TBuffer>();
			if (!(gameWorld.Boards.ComponentType.ComponentBoardColumns[(int) componentType.Id] is BufferComponentBoard bufferComponentBoard))
				return;

			var rawBufferSpan = bufferComponentBoard.AsSpan();
			for (var i = 0; i != rawBufferSpan.Length; i++)
			{
				var buffer = new ComponentBuffer<TBuffer>(rawBufferSpan[i]);
				KeepAlive(keep, buffer.Span, MemoryMarshal.Cast<GameEntity, GameResource<TResource>>(resources));
			}
		}

		protected abstract void KeepAlive(Span<bool> keep, Span<TBuffer> self, Span<GameResource<TResource>> resources);
	}
}