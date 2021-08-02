using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Collections.Pooled;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.HLAPI;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.Utility.EntityQuery;

namespace StormiumTeam.GameBase.Utility.Misc.EntitySystem
{
	public struct SystemState<T>
	{
		public GameWorld World;
		public T         Data;

		public void Deconstruct(out GameWorld gameWorld, out T data)
		{
			gameWorld = World;
			data      = Data;
		}
	}

	public class ArchetypeSystem<T> : IBatch
	{
		public readonly bool ForceSingleThread;

		public delegate void OnArchetype(in ReadOnlySpan<GameEntityHandle> entities, in SystemState<T> state);

		private OnArchetype action;
		private EntityQuery query;

		public ArchetypeSystem(OnArchetype action, EntityQuery query, bool forceSingleThread = false)
		{
			this.action = action;
			this.query  = query;

			ForceSingleThread = forceSingleThread;

			currentState = default;
		}

		private SystemState<T> currentState;

		public void PrepareData(T data)
		{
			query.CheckForNewArchetypes();

			currentState.World = query.GameWorld;
			currentState.Data  = data;
		}

		private int entityCount;
		//private PooledList<GameEntityHandle> batchHandles = new (64);

		private int GetEntityToProcess(int taskCount)
		{
			return Math.Max((int)Math.Ceiling((float)entityCount / taskCount), 1);
		}

		public int PrepareBatch(int taskCount)
		{
			currentState.World = query.GameWorld;
			query.CheckForNewArchetypes();

			entityCount = query.GetEntityCount();

			return entityCount == 0
				? 0
				: ForceSingleThread
					? 1
					: Math.Max((int)Math.Ceiling((float)entityCount / GetEntityToProcess(taskCount)), 1);
		}

		public void Execute(int index, int maxUseIndex, int task, int taskCount)
		{
			int start, end;
			if (ForceSingleThread)
			{
				start = 0;
				end   = entityCount;
			}
			else
			{
				var entityToProcess = GetEntityToProcess(taskCount);

				var batchSize = index == maxUseIndex
					? entityCount - (entityToProcess * index)
					: entityToProcess;

				start = entityToProcess * index;
				end   = start + batchSize;

				if (start >= entityCount)
					return;
			}

			var count = Math.Min(entityCount, end) - start;
			var board = query.GameWorld.Boards.Archetype;
			foreach (var archetype in query.Archetypes)
			{
				var span = board.GetEntities(archetype);
				// Decrease start until it goes in the negative
				// The reason why it's like that is to not introduce another variable with the role of a counter
				start -= span.Length;
				if (start < 0)
				{
					// Get a slice of entities from start and count
					var slice = MemoryMarshal.Cast<uint, GameEntityHandle>(span)
					                         .Slice(start + span.Length, Math.Min(span.Length - (start + span.Length), count));
					action(slice, currentState);
					// Decrease count by the result length
					// If it's superior than 0 this mean we need to go onto the next archetype
					count -= slice.Length;

					// List exhausted, terminate
					if (count <= 0)
						break;

					start = 0; // Next iteration will start on 0
				}
			}
		}
	}
}