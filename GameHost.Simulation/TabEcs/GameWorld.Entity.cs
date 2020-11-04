using System;
using System.Runtime.InteropServices;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld
	{
		public GameEntity CreateEntity()
		{
			return new GameEntity(Boards.Entity.CreateRow());
		}

		public void CreateEntityBulk(Span<GameEntity> entities)
		{
			Boards.Entity.CreateRowBulk(MemoryMarshal.Cast<GameEntity, uint>(entities));
		}

		public void RemoveEntity(GameEntity entity)
		{
			foreach (ref readonly var componentType in Boards.ComponentType.Registered)
				RemoveComponent(entity, componentType);

			foreach (ref readonly var linkedEntity in Boards.Entity.GetLinkedEntities(entity.Id))
			{
				if (Contains(linkedEntity))
					RemoveEntity(linkedEntity);
			}

			foreach (ref readonly var parent in Boards.Entity.GetLinkedParents(entity.Id))
			{
				if (Contains(parent))
					Boards.Entity.RemoveLinked(parent.Id, entity.Id);
			}

			var archetype = GetArchetype(entity);
			if (archetype.Id > 0)
			{
				Boards.Archetype.RemoveEntity(archetype.Id, entity.Id);
				// Reset archetype of this ID.
				// Since we share the total entity span on clients, the client should know that the entity is deleted via its archetype
				Boards.Entity.ArchetypeColumn[(int) entity.Id] = default;
			}
			
			Boards.Entity.DeleteRow(entity.Id);
		}

		public EntityArchetype GetArchetype(GameEntity entity)
		{
			return Boards.Entity.ArchetypeColumn[(int) entity.Id];
		}

		public bool Contains(GameEntity entity)
		{
			return Boards.Entity.ArchetypeColumn[(int) entity.Id].Id > 0;
		}

		/// <summary>
		/// Set if a child entity should be linked to an owner entity. If the owner get removed, the child will too.
		/// </summary>
		/// <param name="child"></param>
		/// <param name="owner"></param>
		/// <param name="isLinked"></param>
		/// <returns>Return if the linking state has been changed</returns>
		public bool Link(GameEntity child, GameEntity owner, bool isLinked)
		{
			return isLinked
				? Boards.Entity.AddLinked(owner.Id, child.Id)
				: Boards.Entity.RemoveLinked(owner.Id, child.Id);
		}
	}
}