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

			Boards.Entity.DeleteRow(entity.Id);
		}
	}
}