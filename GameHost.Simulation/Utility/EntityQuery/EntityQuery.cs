﻿using System;
using System.Buffers;
using System.Collections.Generic;
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

		public Span<uint> Archetypes => matchedArchetypes.Span;

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
				Board      = GameWorld.Boards.Archetype,
				Inner      = matchedArchetypes,
				InnerIndex = -1,
				InnerSize  = matchedArchetypes.Count
			};
		}

		// we need to make sure that the user know to not call this method at each iteration of a loop (eg: `for (i = 0; i < GetEntityCount(); i++)`)
		public int GetEntityCount()
		{
			CheckForNewArchetypes();

			var count = 0;
			// It is fast?
			foreach (var arch in matchedArchetypes.Span)
				count += GameWorld.Boards.Archetype.GetEntities(arch).Length;
			return count;
		}

		public bool Any()
		{
			CheckForNewArchetypes();

			foreach (var arch in matchedArchetypes.Span)
				if (!GameWorld.Boards.Archetype.GetEntities(arch).IsEmpty)
					return true;
			return false;
		}

		public void AddEntitiesTo(PooledList<GameEntityHandle> list)
		{
			foreach (var archetype in matchedArchetypes.Span)
			{
				list.AddRange(MemoryMarshal.Cast<uint, GameEntityHandle>(GameWorld.Boards.Archetype.GetEntities(archetype)));
			}
		}

		public void AddEntitiesTo<TList>(TList list)
			where TList : IList<GameEntityHandle>
		{
			foreach (var archetype in matchedArchetypes.Span)
			{
				foreach (ref readonly var entity in MemoryMarshal.Cast<uint, GameEntityHandle>(GameWorld.Boards.Archetype.GetEntities(archetype)))
				{
					list.Add(entity);
				}
			}
		}

		/// <summary>
		/// Remove all entities that match this query
		/// </summary>
		public void RemoveAllEntities()
		{
			CheckForNewArchetypes();

			foreach (var arch in matchedArchetypes.Span)
			{
				var entitySpan = MemoryMarshal.Cast<uint, GameEntityHandle>(GameWorld.Boards.Archetype.GetEntities(arch));
				var rented     = ArrayPool<GameEntityHandle>.Shared.Rent(entitySpan.Length);

				try
				{
					entitySpan.CopyTo(rented);
					GameWorld.RemoveEntityBulk(rented.AsSpan(0, entitySpan.Length), true);
				}
				finally
				{
					ArrayPool<GameEntityHandle>.Shared.Return(rented);
				}
			}
		}

		/// <summary>
		/// Check if this entity can be contained in this query
		/// </summary>
		/// <param name="entityHandle">The entity</param>
		/// <returns></returns>
		public bool MatchAgainst(GameEntityHandle entityHandle)
		{
			#if DEBUG
			if (entityHandle.Id > 0) GameWorld.ThrowOnInvalidHandle(entityHandle);

			var archetype = GameWorld.GetArchetype(entityHandle).Id;
			if (archetype >= archetypeIsValid.Length)
			{
				if (GameWorld.Boards.Archetype.Registered.Length > lastArchetypeCount)
					throw new InvalidOperationException($"{nameof(CheckForNewArchetypes)} was not called and resulted in an archetype out of bounds in this query. (arch={archetype}, lastCount={lastArchetypeCount}, currentCount={GameWorld.Boards.Archetype.Registered.Length})");

				throw new IndexOutOfRangeException($"Archetype '{archetype}' is out of bounds from '{archetypeIsValid.Length}'");
			}
			
			#endif
			return archetypeIsValid[GameWorld.GetArchetype(entityHandle).Id];
		}

		public void Dispose()
		{
			matchedArchetypes.Dispose();
			archetypeIsValid = Array.Empty<bool>();
		}
	}
}