﻿using System;
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
		public readonly ComponentType[] None;

		public FinalizedQuery AsFinalized => new FinalizedQuery {All = All, None = None};

		// Since archetypes can't be deleted, we can just check if an archetype has been added with the length
		private int lastArchetypeCount;

		private PooledList<uint> matchedArchetypes;
		private bool[]           archetypeIsValid;

		public EntityQuery(GameWorld gameWorld, FinalizedQuery query)
		{
			GameWorld = gameWorld;
			All       = query.All.ToArray();
			None      = query.None.ToArray();

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

		/// <summary>
		/// Check for new valid archetypes
		/// </summary>
		public bool CheckForNewArchetypes()
		{
			var archetypeBoard = GameWorld.Boards.Archetype;
			var newLength      = archetypeBoard.Registered.Length;
			if (lastArchetypeCount == newLength)
				return false;

			Array.Resize(ref archetypeIsValid, newLength);

			for (var i = lastArchetypeCount - 1; i < newLength; i++)
			{
				var archetype     = archetypeBoard.Registered[i];
				var matches       = 0;
				var componentSpan = archetypeBoard.GetComponentTypes(archetype.Id);
				for (var comp = 0; comp != All.Length; comp++)
				{
					if (componentSpan.Contains(All[comp].Id))
						matches++;
				}

				if (matches != All.Length)
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
		/// Get the entities from valid archetypes
		/// </summary>
		/// <returns></returns>
		public EntityQueryEnumerator GetEntities()
		{
			CheckForNewArchetypes();

			return new EntityQueryEnumerator
			{
				Board = GameWorld.Boards.Archetype,
				Inner = matchedArchetypes.Span.GetEnumerator()
			};
		}

		/// <summary>
		/// Check if this entity can be contained in this query
		/// </summary>
		/// <param name="entity">The entity</param>
		/// <returns></returns>
		public bool MatchAgainst(GameEntity entity)
		{
			return archetypeIsValid[GameWorld.GetArchetype(entity).Id];
		}

		public void Dispose()
		{
			matchedArchetypes.Dispose();
			archetypeIsValid = Array.Empty<bool>();
		}
	}
}