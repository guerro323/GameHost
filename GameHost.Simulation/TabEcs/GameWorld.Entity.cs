using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld
	{
		public GameEntityHandle CreateEntity()
		{
			return new GameEntityHandle(Boards.Entity.CreateRow());
		}

		public void CreateEntityBulk(Span<GameEntityHandle> entities)
		{
			Boards.Entity.CreateRowBulk(MemoryMarshal.Cast<GameEntityHandle, uint>(entities));
		}

		public void RemoveEntity(GameEntityHandle entityHandle)
		{
			foreach (ref readonly var componentType in Boards.ComponentType.Registered)
				RemoveComponent(entityHandle, componentType);

			foreach (ref readonly var linkedEntity in Boards.Entity.GetLinkedEntities(entityHandle.Id))
			{
				if (Contains(linkedEntity))
					RemoveEntity(linkedEntity);
			}

			foreach (ref readonly var parent in Boards.Entity.GetLinkedParents(entityHandle.Id))
			{
				if (Contains(parent))
					Boards.Entity.RemoveLinked(parent.Id, entityHandle.Id);
			}

			var archetype = GetArchetype(entityHandle);
			if (archetype.Id > 0)
			{
				Boards.Archetype.RemoveEntity(archetype.Id, entityHandle.Id);
				// Reset archetype of this ID.
				// Since we share the total entity span on clients, the client should know that the entity is deleted via its archetype
				Boards.Entity.ArchetypeColumn[(int) entityHandle.Id] = default;
			}

			Boards.Entity.DeleteRow(entityHandle.Id);
		}

		public EntityArchetype GetArchetype(GameEntityHandle entityHandle)
		{
			return Boards.Entity.ArchetypeColumn[(int) entityHandle.Id];
		}

		/// <summary>
		/// Whether or not this entity handle is valid in the boards.
		/// </summary>
		public bool Contains(GameEntityHandle entityHandle)
		{
			return Boards.Entity.ArchetypeColumn[(int) entityHandle.Id].Id > 0;
		}

		/// <summary>
		/// Whether or not this entity currently exist (handle & version)
		/// </summary>
		/// <param name="entity"></param>
		/// <returns></returns>
		public bool Exists(GameEntity entity)
		{
			unchecked
			{
				return Contains(entity.Handle) && Boards.Entity.VersionColumn[(int) entity.Id] == entity.Version;
			}
		}

		/// <summary>
		/// Get a safe version of the handle (aka with version)
		/// </summary>
		/// <param name="handle"></param>
		/// <returns></returns>
		/// <remarks>
		///	It may also be possible that you have an invalid handle, and that you want to check for an updated one.
		/// For example: oldEntity.Version != gameWorld.Safe(oldEntity.Handle).Version
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameEntity Safe(GameEntityHandle handle)
		{
			unchecked
			{
				return new GameEntity(handle.Id, Boards.Entity.VersionColumn[(int) handle.Id]);
			}
		}

		/// <summary>
		/// Set if a child entity should be linked to an owner entity. If the owner get removed, the child will too.
		/// </summary>
		/// <param name="child"></param>
		/// <param name="owner"></param>
		/// <param name="isLinked"></param>
		/// <returns>Return if the linking state has been changed</returns>
		public bool Link(GameEntityHandle child, GameEntityHandle owner, bool isLinked)
		{
			return isLinked
				? Boards.Entity.AddLinked(owner.Id, child.Id)
				: Boards.Entity.RemoveLinked(owner.Id, child.Id);
		}
	}
}