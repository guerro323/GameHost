using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Collections.Pooled;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	/// <summary>
	/// An <see cref="EntityQuery"/> contains information about entities archetype that matches with requested components.
	/// </summary>
	public class EntityQuery : IDisposable
	{
		public readonly GameWorld       GameWorld;
		public readonly ComponentType[] All;
		public readonly ComponentType[] Or;
		public readonly ComponentType[] None;

		public FinalizedQuery AsFinalized => new FinalizedQuery {All = All, Or = Or, None = None};

		// Since archetypes can't be deleted, we can just check if an archetype has been added with the length
		private int lastArchetypeCount;

		private PooledList<uint> matchedArchetypes;
		private bool[]           archetypeIsValid;

		public EntityQuery(GameWorld gameWorld, FinalizedQuery query)
		{
			GameWorld = gameWorld;
			All       = query.All.ToArray();
			None      = query.None.ToArray();
			Or        = query.Or.ToArray();

			matchedArchetypes = new PooledList<uint>();
			archetypeIsValid  = Array.Empty<bool>();
		}

		public EntityQuery(GameWorld gameWorld, Span<ComponentType> all)
			: this(gameWorld, new FinalizedQuery {All = all})
		{

		}

		public EntityQuery(GameWorld gameWorld, Span<ComponentType> all, Span<ComponentType> none)
			: this(gameWorld, new FinalizedQuery {All = all, None = none})
		{

		}

		public EntityQuery(GameWorld gameWorld, Span<ComponentType> all, Span<ComponentType> none, Span<ComponentType> or)
			: this(gameWorld, new FinalizedQuery {All = all, None = none, Or = or})
		{

		}

		/// <summary>
		/// Check for new valid archetypes
		/// </summary>
		public bool CheckForNewArchetypes()
		{
			var archetypeBoard = GameWorld.Boards.Archetype;
			var newLength      = archetypeBoard.Registered.Length;
			if (lastArchetypeCount == newLength)
				return false;

			Array.Resize(ref archetypeIsValid, newLength + 1);

			for (var i = lastArchetypeCount; i < newLength; i++)
			{
				var archetype     = archetypeBoard.Registered[i];
				var matches       = 0;
				var orMatches     = 0;
				var componentSpan = archetypeBoard.GetComponentTypes(archetype.Id);
				for (var comp = 0; comp != All.Length; comp++)
				{
					if (componentSpan.Contains(All[comp].Id))
						matches++;
				}

				for (var comp = 0; comp != Or.Length; comp++)
				{
					if (componentSpan.Contains(Or[comp].Id))
						orMatches++;
				}

				if (matches != All.Length || (Or.Length > 0 && orMatches == 0))
					continue;

				matches = 0;
				for (var comp = 0; comp != None.Length && matches == 0; comp++)
				{
					if (componentSpan.Contains(None[comp].Id))
						matches++;
				}

				if (matches > 0)
					continue;

				matchedArchetypes.Add(archetype.Id);
				archetypeIsValid[archetype.Id] = true;
			}

			lastArchetypeCount = newLength;
			return true;
		}

		/// <summary>
		/// Get the entities from valid archetypes, with an option to swapback entities
		/// </summary>
		public unsafe EntityQueryEnumerator GetEnumerator()
		{
			CheckForNewArchetypes();

			ref var r = ref MemoryMarshal.GetReference(matchedArchetypes.Span);
			return new EntityQueryEnumerator
			{
				Board       = GameWorld.Boards.Archetype,
				Inner       = matchedArchetypes,
				InnerIndex  = -1,
				InnerSize   = matchedArchetypes.Count
			};
		}

		public bool Any()
		{
			CheckForNewArchetypes();

			foreach (var arch in matchedArchetypes.Span)
				if (!GameWorld.Boards.Archetype.GetEntities(arch).IsEmpty)
					return true;
			return false;
		}

		public void RemoveAllEntities()
		{
			CheckForNewArchetypes();

			foreach (var arch in matchedArchetypes.Span)
			{
				// This work on a swapback basis, so we need to decrement by one at each delete
				while (GameWorld.Boards.Archetype.GetEntities(arch).Length > 0)
					GameWorld.RemoveEntity(new GameEntityHandle(GameWorld.Boards.Archetype.GetEntities(arch)[0]));
			}
		}

		/// <summary>
		/// Check if this entity can be contained in this query
		/// </summary>
		/// <param name="entityHandle">The entity</param>
		/// <returns></returns>
		public bool MatchAgainst(GameEntityHandle entityHandle)
		{
			return archetypeIsValid[GameWorld.GetArchetype(entityHandle).Id];
		}

		public void Dispose()
		{
			matchedArchetypes.Dispose();
			archetypeIsValid = Array.Empty<bool>();
		}
	}
}