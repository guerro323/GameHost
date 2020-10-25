using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Simulation.Application;
using GameHost.Simulation.TabEcs;
using K4os.Compression.LZ4;
using RevolutionSnapshot.Core.Buffers;
using Array = System.Array;

namespace GameHost.Simulation.Features.ShareWorldState
{
	[RestrictToApplication(typeof(SimulationApplication))]
	public class SendWorldStateSystem : AppSystemWithFeature<ShareWorldStateFeature>
	{
		private GameWorld gameWorld;

		private (IShareComponentSerializer[] serializer, byte h) custom_column;
		private DataBufferWriter                                 dataBuffer;
		private DataBufferWriter                                 compressedBuffer;

		private ParallelOptions parallelOptions;
		
		public SendWorldStateSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref gameWorld);

			parallelOptions     = new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount};
			getDataParallelBody = GetDataParallel_Body;

			custom_column.serializer = new IShareComponentSerializer[0];
			dataBuffer               = new DataBufferWriter(0);
			compressedBuffer         = new DataBufferWriter(0);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			
			GetDataParallel(gameWorld);

			compressedBuffer.Length = 0;

			var compressedSize = LZ4Codec.MaximumOutputSize(dataBuffer.Length);
			if (compressedSize + sizeof(int) * 2 > compressedBuffer.Capacity)
				compressedBuffer.Capacity = compressedSize + sizeof(int) * 2;

			unsafe
			{
				var originalCapacity = compressedBuffer.Capacity;
				var encoder          = LZ4Level.L04_HC;
				var size = LZ4Codec.Encode(dataBuffer.Span,
					new Span<byte>((byte*) compressedBuffer.GetSafePtr() + sizeof(int) * 2, compressedBuffer.Capacity - sizeof(int) * 2), encoder);
				compressedBuffer.WriteInt(size);
				compressedBuffer.WriteInt(dataBuffer.Length);
				compressedBuffer.Length += size;

				if (originalCapacity < compressedBuffer.Capacity)
					throw new InvalidOperationException("The capacity shouldn't have been modified. This does remove the compression data.");
			}
			
			foreach (var feature in Features)
			{
				unsafe
				{
					feature.Transport.Broadcast(default, compressedBuffer.Span);
				}
			}
		}

		public override void Dispose()
		{
			base.Dispose();

			dataBuffer.Dispose();
		}

		public void SetComponentSerializer(ComponentType componentType, IShareComponentSerializer serializer)
		{
			if (componentType.Id + 1 >= custom_column.serializer.Length)
				Array.Resize(ref custom_column.serializer, (int) componentType.Id + 1);

			custom_column.serializer[(int) componentType.Id] = serializer;
		}

		private Action<StateData, ParallelLoopState, long> getDataParallelBody;
		private List<StateData> pooledStates = new List<StateData>();

		private struct StateData
		{
			public DataBufferWriter[] Writers;
			public GameWorld          World;
			public ComponentType      ComponentType;
		}
		
		void GetDataParallel_Body(StateData state, ParallelLoopState loop, long i)
		{
			var buffer   = state.Writers[i] = new DataBufferWriter(128);
			var entities = state.World.Boards.Entity.Alive;

			Inner(ref buffer, state.World, state.ComponentType.Id, entities);
		}

		private unsafe DataBufferWriter GetDataParallel(GameWorld world, bool forceSingleThread = false)
		{
			dataBuffer.Length = 0;

			// 1. Write Component types
			var componentTypeSpan = world.Boards.ComponentType.Registered;
			dataBuffer.WriteInt(componentTypeSpan.Length);
			if (componentTypeSpan.Length > 0)
			{
				dataBuffer.WriteSpan(componentTypeSpan);

				// 1.5 Write description of component type
				foreach (var componentType in componentTypeSpan)
				{
					dataBuffer.WriteInt(world.Boards.ComponentType.SizeColumns[(int) componentType.Id]);
					dataBuffer.WriteStaticString(world.Boards.ComponentType.NameColumns[(int) componentType.Id]);
				}
			}

			// 2. Write archetypes
			var archetypes = world.Boards.Archetype.Registered;
			dataBuffer.WriteInt(archetypes.Length);
			if (archetypes.Length > 0)
			{
				dataBuffer.WriteSpan(archetypes);

				// 2.1 write registered components of each archetype
				for (var i = 0; i != archetypes.Length; i++)
				{
					var row               = archetypes[i].Id;
					var archetypeTypeSpan = world.Boards.Archetype.GetComponentTypes(row);
					dataBuffer.WriteInt(archetypeTypeSpan.Length);
					dataBuffer.WriteSpan(archetypeTypeSpan);
				}
			}
			else
			{
				return dataBuffer;
			}

			// 3. Write entities
			var entities = world.Boards.Entity.Alive;
			dataBuffer.WriteInt(entities.Length);
			if (entities.Length > 0)
			{
				// 3.1
				dataBuffer.WriteSpan(entities);
				// 3.2 archetypes
				dataBuffer.WriteInt(world.Boards.Entity.ArchetypeColumn.Length); // the length of the column is important since it can be bigger than the alive entities.
				dataBuffer.WriteSpan(world.Boards.Entity.ArchetypeColumn);
			}
			else
			{
				return dataBuffer;
			}

			// 4. Write components
			var componentBuffers = new DataBufferWriter[componentTypeSpan.Length];
			if (forceSingleThread)
			{
				for (var i = 0; i != componentTypeSpan.Length; i++)
				{
					var buffer = componentBuffers[i] = new DataBufferWriter(128);
					Inner(ref buffer, world, componentTypeSpan[i].Id, entities);
				}
			}
			else
			{
				pooledStates.Clear();
				for (var i = 0; i != componentTypeSpan.Length; i++)
				{
					pooledStates.Add(new StateData
					{
						Writers = componentBuffers,
						World = world,
						ComponentType = componentTypeSpan[i]
					});
				}

				Parallel.ForEach(pooledStates, parallelOptions, getDataParallelBody);
			}

			var capacityIncrease = 0;
			foreach (var buffer in componentBuffers)
				capacityIncrease += buffer.Capacity;

			dataBuffer.Capacity = Math.Max(dataBuffer.Capacity, dataBuffer.Length + capacityIncrease + 1);
			foreach (var buffer in componentBuffers)
			{
				dataBuffer.WriteBuffer(buffer);
				buffer.Dispose();
			}

			return dataBuffer;
		}

		private unsafe void Inner(ref DataBufferWriter buffer, GameWorld world, uint row, Span<GameEntity> entities)
		{
			byte* ptr<T>(Span<T> span)
			{
				return (byte*) Unsafe.AsPointer(ref span.GetPinnableReference());
			}

			var skipMarker = buffer.WriteInt(0);
			var serializer = world.Boards.ComponentType.GetColumn(row, ref custom_column.serializer);

			var componentBoard = world.Boards.ComponentType.ComponentBoardColumns[(int) row];
			buffer.Capacity += (sizeof(EntityBoardContainer.ComponentMetadata) + sizeof(int) + componentBoard.Size) * entities.Length + 64;

			if (serializer != null && serializer.CanSerialize(world, entities, componentBoard))
			{
				serializer.SerializeBoard(ref buffer, world, entities, componentBoard);
			}
			else
				switch (componentBoard)
				{
					case TagComponentBoard tagComponentBoard:
						// yep. nothing.
						break;
					case SingleComponentBoard singleComponentBoard:
					{
						var linkColumnSpan = world.Boards.Entity.GetComponentColumn(row);

						buffer.WriteInt(linkColumnSpan.Length);
						buffer.WriteDataSafe(ptr(linkColumnSpan), linkColumnSpan.Length * sizeof(EntityBoardContainer.ComponentMetadata), default);

						var countMarker = buffer.WriteInt(0);
						var count       = 0;
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

								count++;

								var componentData = singleComponentBoard.ReadRaw(link.Id);
								buffer.WriteDataSafe((byte*) Unsafe.AsPointer(ref componentData.GetPinnableReference()), componentBoard.Size, default);
								break;
							}

							if (recursionLeft == 0)
								throw new InvalidOperationException($"GetComponentData - Recursion limit reached with '{entities[ent]}' and component (backing: {row})");
						}

						buffer.WriteInt(count, countMarker);
						break;
					}
					case BufferComponentBoard bufferComponentBoard:
					{
						var linkColumnSpan = world.Boards.Entity.GetComponentColumn(row);

						buffer.WriteInt(linkColumnSpan.Length);
						buffer.WriteDataSafe(ptr(linkColumnSpan), linkColumnSpan.Length * sizeof(EntityBoardContainer.ComponentMetadata), default);

						var countMarker = buffer.WriteInt(0);
						var count       = 0;
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

								count++;

								var rawBufferData = bufferComponentBoard.AsSpan()[(int) link.Id];
								buffer.WriteInt(rawBufferData.Count);
								buffer.WriteDataSafe((byte*) Unsafe.AsPointer(ref rawBufferData.Span.GetPinnableReference()), rawBufferData.Count, default);
								break;
							}

							if (recursionLeft == 0)
								throw new InvalidOperationException($"GetComponentData - Recursion limit reached with '{entities[ent]}' and component (backing: {row})");
						}

						buffer.WriteInt(count, countMarker);
						break;
					}
				}

			buffer.WriteInt(buffer.Length - skipMarker.Index, skipMarker);
		}
	}
}