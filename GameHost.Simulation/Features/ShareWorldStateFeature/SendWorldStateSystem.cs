using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Collections.Pooled;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Simulation.Application;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.Utility.EntityQuery;
using GameHost.Simulation.Utility.Time;
using K4os.Compression.LZ4;
using RevolutionSnapshot.Core.Buffers;
using Array = System.Array;

namespace GameHost.Simulation.Features.ShareWorldState
{
	[DontInjectSystemToWorld]
	[RestrictToApplication(typeof(SimulationApplication))]
	public class SendWorldStateSystem : AppSystemWithFeature<ShareWorldStateFeature>
	{
		private GameWorld gameWorld;

		private (IShareComponentSerializer[] serializer, bool[] disabled, byte h) custom_column;
		private DataBufferWriter                                                  dataBuffer;
		private DataBufferWriter                                                  compressedBuffer;

		private ParallelOptions parallelOptions;
		
		public SendWorldStateSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref gameWorld);

			parallelOptions     = new ParallelOptions {MaxDegreeOfParallelism = Environment.ProcessorCount};
			getDataParallelBody = GetDataParallel_Body;

			custom_column.serializer = new IShareComponentSerializer[0];
			custom_column.disabled    = new bool[0];
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

				//Console.WriteLine($"compressed={size}b original={dataBuffer.Length}b");

				if (originalCapacity < compressedBuffer.Capacity)
					throw new InvalidOperationException("The capacity shouldn't have been modified. This does remove the compression data.");
			}

			foreach (var (entity, feature) in Features)
			{
				feature.Transport.Broadcast(default, compressedBuffer.Span);
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

		public void SetDisabled(ComponentType componentType, bool isEnabled)
		{
			if (componentType.Id + 1 >= custom_column.disabled.Length)
				Array.Resize(ref custom_column.disabled, (int) componentType.Id + 1);

			custom_column.disabled[componentType.Id] = isEnabled;
		}

		private Action<StateData, ParallelLoopState, long> getDataParallelBody;
		private List<StateData>                            pooledStates  = new();
		private PooledList<DataBufferWriter>               pooledWriters = new();

		private struct StateData
		{
			public PooledList<DataBufferWriter> Writers;
			public GameWorld      World;
			public ComponentType  ComponentType;
		}

		void GetDataParallel_Body(StateData state, ParallelLoopState loop, long i)
		{
			var buffer   = state.Writers[(int) i] = new DataBufferWriter(128);
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

				var max = 0u;
				
				// 1.5 Write description of component type
				foreach (var componentType in componentTypeSpan)
				{
					dataBuffer.WriteInt(world.Boards.ComponentType.SizeColumns[(int) componentType.Id]);
					dataBuffer.WriteStaticString(world.Boards.ComponentType.NameColumns[(int) componentType.Id]);

					max = Math.Max(componentType.Id, max);
				}
				
				// Make sure that the component column on the entity board are correctly initialized
				// If we don't, there may be a possibility that they may get created in parallel (which we don't want)
				world.Boards.Entity.GetComponentColumn(max);
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
				// 3.3 versions
				dataBuffer.WriteSpan(world.Boards.Entity.VersionColumn);
			}
			else
			{
				return dataBuffer;
			}

			// 4. Write components
			pooledWriters.Clear();
			pooledWriters.AddSpan(componentTypeSpan.Length);
			if (forceSingleThread)
			{
				for (var i = 0; i != componentTypeSpan.Length; i++)
				{
					var buffer = pooledWriters[i] = new DataBufferWriter(128);
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
						Writers = pooledWriters,
						World = world,
						ComponentType = componentTypeSpan[i]
					});
				}

				Parallel.ForEach(pooledStates, parallelOptions, getDataParallelBody);
			}

			var capacityIncrease = 0;
			foreach (var buffer in pooledWriters)
				capacityIncrease += buffer.Capacity;


			dataBuffer.Capacity = Math.Max(dataBuffer.Capacity, dataBuffer.Length + capacityIncrease + 1);

			var biggestIdx       = 0;
			var biggest          = 0;
			var componentBuffers = pooledWriters.Span;
			for (var index = 0; index < componentBuffers.Length; index++)
			{
				var buffer = componentBuffers[index];
				dataBuffer.WriteBuffer(buffer);
				if (buffer.Length > biggest)
				{
					biggest    = buffer.Length;
					biggestIdx = index;
					
					//Console.WriteLine($"  Biggest Buffer Name={gameWorld.Boards.ComponentType.NameColumns[(int) componentTypeSpan[biggestIdx].Id]} Size={biggest}");
				}

				buffer.Dispose();
			}

			return dataBuffer;
		}

		private unsafe void Inner(ref DataBufferWriter buffer, GameWorld world, uint row, Span<GameEntityHandle> entities)
		{
			var skipMarker = buffer.WriteInt(0);
			var serializer = world.Boards.ComponentType.GetColumn(row, ref custom_column.serializer);
			var isDisabled  = world.Boards.ComponentType.GetColumn(row, ref custom_column.disabled);

			var componentBoard = world.Boards.ComponentType.ComponentBoardColumns[(int) row];
			buffer.Capacity += (sizeof(EntityBoardContainer.ComponentMetadata) + sizeof(int) + componentBoard.Size) * entities.Length + 64;

			if (serializer != null && serializer.CanSerialize(world, entities, componentBoard))
			{
				serializer.SerializeBoard(ref buffer, world, entities, componentBoard);
			}
			else if (isDisabled)
			{
				buffer.WriteInt(0);
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
						
						var countMarker = buffer.WriteInt(0);
						var count       = 0;
						for (var ent = 0; ent != entities.Length && ent < linkColumnSpan.Length; ent++)
						{
							var entity = entities[ent];
							var link   = linkColumnSpan[(int) entity.Id];
							if (link.Null)
								continue;

							count++;

							var componentData = singleComponentBoard.ReadRaw(link.Id);
							buffer.WriteDataSafe(componentData.Slice(0, componentBoard.Size), default);
						}

						buffer.WriteInt(count, countMarker);
						break;
					}
					case BufferComponentBoard bufferComponentBoard:
					{
						var linkColumnSpan = world.Boards.Entity.GetComponentColumn(row);
						
						var countMarker = buffer.WriteInt(0);
						var count       = 0;
						for (var ent = 0; ent != entities.Length && ent < linkColumnSpan.Length; ent++)
						{
							var entity = entities[ent];
							var link   = linkColumnSpan[(int) entity.Id];
							if (link.Null)
								continue;

							count++;

							var rawBufferData = bufferComponentBoard.AsSpan()[(int) link.Id];
							buffer.WriteInt(rawBufferData.Count);
							buffer.WriteDataSafe(rawBufferData.Span, default);
						}

						buffer.WriteInt(count, countMarker);
						break;
					}
				}

			buffer.WriteInt(buffer.Length - skipMarker.Index, skipMarker);
		}
	}
}