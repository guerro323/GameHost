using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Core.Features;
using GameHost.Core.Features.Systems;
using GameHost.Simulation.TabEcs;
using NetFabric.Hyperlinq;
using RevolutionSnapshot.Core.Buffers;
using Array = System.Array;

namespace GameHost.Simulation.Features.ShareWorldState
{
	public class SendWorldStateSystem : AppSystemWithFeature<ShareWorldStateFeature>
	{
		private GameWorld gameWorld;

		private (IShareComponentSerializer[] serializer, byte h) custom_column;

		public SendWorldStateSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref gameWorld);

			custom_column.serializer = new IShareComponentSerializer[0];
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			var data = GetDataParallel(gameWorld);
			foreach (var feature in Features)
			{
				unsafe
				{
					feature.Transport.Broadcast(default, new ReadOnlySpan<byte>((void*) data.GetSafePtr(), data.Length));
				}
			}

			data.Dispose();

			//Console.WriteLine(data.Length);
		}

		public void SetComponentSerializer(ComponentType componentType, IShareComponentSerializer serializer)
		{
			if (componentType.Id + 1 >= custom_column.serializer.Length)
				Array.Resize(ref custom_column.serializer, (int) componentType.Id + 1);

			custom_column.serializer[(int) componentType.Id] = serializer;
		}

		private unsafe DataBufferWriter GetData(GameWorld world)
		{
			var buffer = new DataBufferWriter(world.Boards.Entity.Alive.Length * sizeof(long));

			var entities = world.Boards.Entity.Alive;

			buffer.WriteInt(entities.Length);
			if (entities.Length > 0)
				buffer.WriteDataSafe((byte*) Unsafe.AsPointer(ref entities.GetPinnableReference()), sizeof(GameEntity), default);

			var componentTypeSpan = world.Boards.ComponentType.Registered;
			buffer.WriteInt(componentTypeSpan.Length);
			if (componentTypeSpan.Length > 0)
			{
				buffer.WriteDataSafe((byte*) Unsafe.AsPointer(ref componentTypeSpan.GetPinnableReference()), sizeof(ComponentType), default);
				for (var t = 0; t != componentTypeSpan.Length; t++)
				{
					Inner(ref buffer, world, componentTypeSpan[t].Id, entities);
				}
			}

			return buffer;
		}

		private unsafe DataBufferWriter GetDataParallel(GameWorld world)
		{
			byte* ptr<T>(Span<T> span)
			{
				return (byte*) Unsafe.AsPointer(ref span.GetPinnableReference());
			}

			var dataBuffer = new DataBufferWriter(world.Boards.Entity.Alive.Length * sizeof(long));

			var entities = world.Boards.Entity.Alive;

			dataBuffer.WriteInt(entities.Length);
			if (entities.Length > 0)
			{
				dataBuffer.WriteDataSafe(ptr(entities), entities.Length * sizeof(GameEntity), default);
			}

			var componentTypeSpan = world.Boards.ComponentType.Registered;
			dataBuffer.WriteInt(componentTypeSpan.Length);
			if (componentTypeSpan.Length > 0)
			{
				dataBuffer.WriteDataSafe(ptr(componentTypeSpan), componentTypeSpan.Length * sizeof(ComponentType), default);

				var componentBuffers = new DataBufferWriter[componentTypeSpan.Length];
				Parallel.ForEach(componentTypeSpan.ToArray(), (componentType, loop, i) =>
				{
					var buffer   = componentBuffers[i] = new DataBufferWriter(128);
					var entities = world.Boards.Entity.Alive;

					Inner(ref buffer, world, componentType.Id, entities);
				});

				foreach (var buffer in componentBuffers)
				{
					dataBuffer.WriteBuffer(buffer);
				}
			}

			return dataBuffer;
		}

		private unsafe void Inner(ref DataBufferWriter buffer, GameWorld world, uint row, Span<GameEntity> entities)
		{
			byte* ptr<T>(Span<T> span)
			{
				return (byte*) Unsafe.AsPointer(ref span.GetPinnableReference());
			}

			buffer.WriteInt(world.Boards.ComponentType.SizeColumns[(int) row]);
			buffer.WriteString(world.Boards.ComponentType.NameColumns[(int) row]);

			var serializer = world.Boards.ComponentType.GetColumn(row, ref custom_column.serializer);

			var componentBoard = world.Boards.ComponentType.ComponentBoardColumns[(int) row];
			buffer.Capacity += componentBoard.Size * entities.Length;

			if (serializer != null && serializer.CanSerialize(world, entities, componentBoard))
			{
				serializer.SerializeBoard(ref buffer, world, entities, componentBoard);
			}
			else if (componentBoard is SingleComponentBoard singleComponentBoard)
			{
				var linkColumnSpan = world.Boards.Entity.GetComponentColumn(row);

				buffer.WriteInt(linkColumnSpan.Length);
				buffer.WriteDataSafe(ptr(linkColumnSpan), linkColumnSpan.Length * sizeof(EntityBoardContainer.ComponentMetadata), default);
				for (var ent = 0; ent != entities.Length && ent < linkColumnSpan.Length; ent++)
				{
					var entity = entities[ent];
					if (linkColumnSpan[(int) entity.Id].Null)
						continue;
					
					// Recursive support for shared components.

					var recursionLeft = GameWorld.RecursionLimit;
					while (recursionLeft-- > 0)
					{
						var link = linkColumnSpan[(int) entity.Id];
						if (link.IsShared)
						{
							entity = new GameEntity(link.Entity);
							continue;
						}

						var componentData = singleComponentBoard.ReadRaw(link.Id);
						buffer.WriteDataSafe((byte*) Unsafe.AsPointer(ref componentData.GetPinnableReference()), componentBoard.Size, default);
						break;
					}

					if (recursionLeft == 0)
						throw new InvalidOperationException($"GetComponentData - Recursion limit reached with '{entities[ent]}' and component (backing: {row})");
				}
			}
		}
	}
}