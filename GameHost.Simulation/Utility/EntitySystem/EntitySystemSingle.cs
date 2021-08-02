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
			while (query.EntitySliceAt(ref start, ref count, out var span))
			{
				action(span, currentState);
			}
		}

		/// <summary>
		/// Update the system for one entity
		/// </summary>
		public void Update(GameEntityHandle handle, T state)
		{
			action(stackalloc GameEntityHandle[] { handle }, new() { Data = state, World = query.GameWorld });
		}

		public void Update(T state)
		{
			var start = 0;
			var count = query.GetEntityCount();
			while (query.EntitySliceAt(ref start, ref count, out var span))
				action(span, new() { Data = state, World = query.GameWorld });
		}
	}
}