using System;
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

		private PooledList<GameEntityHandle> batchHandles = new (64);

		private int GetEntityToProcess(int taskCount)
		{
			return Math.Max((int) Math.Ceiling((float) batchHandles.Count / taskCount), 1);
		}

		public int PrepareBatch(int taskCount)
		{
			currentState.World = query.GameWorld;
			query.CheckForNewArchetypes();

			batchHandles.Clear();

			var targetCount = query.GetEntityCount();
			if (targetCount > batchHandles.Capacity)
				batchHandles.Capacity = targetCount;

			query.AddEntitiesTo(batchHandles);

			return batchHandles.Count == 0
				? 0
				: ForceSingleThread
					? 1
					: Math.Max((int) Math.Ceiling((float) batchHandles.Count / GetEntityToProcess(taskCount)), 1);
		}

		public void Execute(int index, int maxUseIndex, int task, int taskCount)
		{
			var handleSpan = batchHandles.Span;

			int start, end;
			if (ForceSingleThread)
			{
				start = 0;
				end   = batchHandles.Count;
			}
			else
			{
				var entityToProcess = GetEntityToProcess(taskCount);

				var batchSize = index == maxUseIndex
					? handleSpan.Length - (entityToProcess * index)
					: entityToProcess;

				start = entityToProcess * index;
				end   = start + batchSize;
				
				if (start >= handleSpan.Length)
					return;
			}

			action(handleSpan.Slice(start, Math.Min(handleSpan.Length, end) - start), currentState);
		}
	}
}