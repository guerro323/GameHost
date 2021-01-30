using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
		public delegate void OnArchetype(in EntityArchetype archetype, in ReadOnlySpan<GameEntityHandle> entities, in SystemState<T> state);

		private OnArchetype action;
		private EntityQuery query;

		public ArchetypeSystem(OnArchetype action, EntityQuery query)
		{
			this.action = action;
			this.query  = query;

			currentState = default;
		}

		private SystemState<T> currentState;

		public void PrepareBatch(T data)
		{
			query.CheckForNewArchetypes();

			currentState.World = query.GameWorld;
			currentState.Data  = data;
		}

		public void Update(T data)
		{
			PrepareBatch(data);
			Update();
		}

		public void Update()
		{
			query.CheckForNewArchetypes();

			var archetypes = query.Archetypes;
			if (archetypes.Length == 0)
				return;

			var archetypeBoard = query.GameWorld.Boards.Archetype;
			foreach (ref readonly var archetypeId in archetypes)
			{
				ReadOnlySpan<GameEntityHandle> entities = MemoryMarshal.Cast<uint, GameEntityHandle>(archetypeBoard.GetEntities(archetypeId));
				action(new EntityArchetype(archetypeId), entities, currentState);
			}
		}

		const int ToProcess = 1; // how much archetype per task

		public int GetBatchCount(int taskCount)
		{
			currentState.World = query.GameWorld;
			query.CheckForNewArchetypes();

			var archetypes = query.Archetypes;
			return archetypes.Length == 0 ? 0 : Math.Max((int) Math.Ceiling((float) archetypes.Length / ToProcess), 1);
		}

		public void Execute(int index, int maxUseIndex, int task, int taskCount)
		{
			var archetypes     = query.Archetypes;
			var archetypeBoard = query.GameWorld.Boards.Archetype;

			int batchSize;
			if (index == maxUseIndex)
			{
				var r = ToProcess * index;
				batchSize = archetypes.Length - r;
			}
			else
				batchSize = ToProcess;

			var start = ToProcess * index;
			var end   = start + batchSize;
			if (start >= archetypes.Length || end > archetypes.Length)
				return;

			for (; start < end; start++)
			{
				ReadOnlySpan<GameEntityHandle> entities = MemoryMarshal.Cast<uint, GameEntityHandle>(archetypeBoard.GetEntities(archetypes[start]));
				action(new EntityArchetype(archetypes[start]), entities, currentState);
			}
		}
	}

	public unsafe class EntitySystemComponent<T, T1> : IBatch
		where T1 : struct, IComponentData
	{
		public delegate void OnEntity(in GameEntityHandle handle, ref T1 component, in SystemState<T> state);
		
		private OnEntity    action;
		private EntityQuery query;

		public EntitySystemComponent(OnEntity action, EntityQuery query)
		{
			this.action = action;
			this.query  = query;

			currentState = default;
		}

		private SystemState<T> currentState;
		public void PrepareBatch(T data)
		{
			query.CheckForNewArchetypes();
			
			currentState.World = query.GameWorld;
			currentState.Data  = data;
		}

		public void Update(T data)
		{
			PrepareBatch(data);
			Update();
		}

		public void Update()
		{
			query.CheckForNewArchetypes();

			var archetypes = query.Archetypes;
			if (archetypes.Length == 0)
				return;

			var archetypeBoard = query.GameWorld.Boards.Archetype;
			var accessor       = new ComponentDataAccessor<T1>(query.GameWorld);
			foreach (ref readonly var archetypeId in archetypes)
			{
				ReadOnlySpan<GameEntityHandle> entities = MemoryMarshal.Cast<uint, GameEntityHandle>(archetypeBoard.GetEntities(archetypeId));

				var state = currentState; // it's faster to copy current state, than to access it outside of the method
				foreach (ref readonly var entity in entities)
				{
					action(in entity, ref accessor[entity], state);
				}
			}
		}

		const int ToProcess = 1; // how much archetype per task

		public int GetBatchCount(int taskCount)
		{
			currentState.World = query.GameWorld;
			query.CheckForNewArchetypes();
			
			var archetypes = query.Archetypes;
			return archetypes.Length == 0 ? 0 : Math.Max((int) Math.Ceiling((float) archetypes.Length / ToProcess), 1);
		}

		public void Execute(int index, int maxUseIndex, int task, int taskCount)
		{
			var archetypes     = query.Archetypes;
			var archetypeBoard = query.GameWorld.Boards.Archetype;
			var accessor       = new ComponentDataAccessor<T1>(query.GameWorld);
			
			int batchSize;
			if (index == maxUseIndex)
			{
				var r = ToProcess * index;
				batchSize = archetypes.Length - r;
			}
			else
				batchSize = ToProcess;

			var start = ToProcess * index;
			var end   = start + batchSize;
			if (start >= archetypes.Length || end > archetypes.Length)
				return;
			
			for (; start < end; start++)
			{
				ReadOnlySpan<GameEntityHandle> entities = MemoryMarshal.Cast<uint, GameEntityHandle>(archetypeBoard.GetEntities(archetypes[start]));
				foreach (ref readonly var entity in entities)
				{
					action(in entity, ref accessor[entity], currentState);
				}
			}
		}
	}
}