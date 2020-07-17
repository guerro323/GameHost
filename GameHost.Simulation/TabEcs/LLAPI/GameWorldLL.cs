using System.Runtime.CompilerServices;

namespace GameHost.Simulation.TabEcs.LLAPI
{
	public static class GameWorldLL
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ComponentBoardBase GetComponentBoardBase(ComponentTypeBoardContainer componentTypeBoardContainer, ComponentType componentType)
		{
			return componentTypeBoardContainer.ComponentBoardColumns[(int) componentType.Id];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint CreateComponent(ComponentBoardBase componentBoard)
		{
			return componentBoard.CreateRow();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AssignComponent(ComponentBoardBase componentBoard, ComponentReference componentReference, EntityBoardContainer entityBoard, GameEntity entity)
		{
			componentBoard.AddReference(componentReference.Id, entity);

			var previousComponentId = entityBoard.AssignComponentReference(entity.Id, componentReference.Type.Id, componentReference.Id);
			if (previousComponentId > 0)
			{
				var refs = componentBoard.RemoveReference(previousComponentId, entity);

				// nobody reference this component anymore, let's remove the row
				if (refs == 0)
					componentBoard.DeleteRow(previousComponentId);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetOwner(ComponentBoardBase componentBoard, ComponentReference componentReference, GameEntity entity)
		{
			componentBoard.OwnerColumn[(int) componentReference.Id] = entity;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool RemoveComponentReference(ComponentBoardBase componentBoard, ComponentType componentType, EntityBoardContainer entityBoard, GameEntity entity)
		{
			// todo: we need to have a real method for removing the component metadata on the column.
			var previousComponentId = entityBoard.AssignComponentReference(entity.Id, componentType.Id, 0);
			if (previousComponentId > 0)
			{
				var refs = componentBoard.RemoveReference(previousComponentId, entity);

				// nobody reference this component anymore, let's remove the row
				if (refs == 0)
					componentBoard.DeleteRow(previousComponentId);
				return true;
			}

			return false;
		}
	}
}