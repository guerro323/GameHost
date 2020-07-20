using System;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.TabEcs.LLAPI;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld
	{
		/// <summary>
		/// Remove a component from an entity.
		/// </summary>
		/// <param name="entity">The entity</param>
		/// <param name="componentType">The component type</param>
		/// <returns>True if the component was removed, false if it did not exist.</returns>
		public bool RemoveMultipleComponent(GameEntity entity, Span<ComponentType> componentTypeSpan)
		{
			var b = true;
			foreach (ref readonly var componentType in componentTypeSpan)
			{
				b &= GameWorldLL.RemoveComponentReference(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType), componentType, Boards.Entity, entity);
			}

			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);

			return b;
		}

		/// <summary>
		/// Remove a component from an entity.
		/// </summary>
		/// <param name="entity">The entity</param>
		/// <param name="componentType">The component type</param>
		/// <returns>True if the component was removed, false if it did not exist.</returns>
		public bool RemoveComponent(GameEntity entity, ComponentType componentType)
		{
			var b = GameWorldLL.RemoveComponentReference(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType), componentType, Boards.Entity, entity);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);

			return b;
		}

		/// <summary>
		/// Assign an existing component to an entity
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="component"></param>
		public void AssignComponent(GameEntity entity, ComponentReference component)
		{
			var board = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, component.Type);
			GameWorldLL.AssignComponent(board, component, Boards.Entity, entity);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);
		}

		/// <summary>
		/// Assign and set as a owner a component to an entity
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="component"></param>
		public void AssignComponentAsOwner(GameEntity entity, ComponentReference component)
		{
			var board = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, component.Type);
			GameWorldLL.AssignComponent(board, component, Boards.Entity, entity);
			GameWorldLL.SetOwner(board, component, entity);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);
		}

		public void DependOnEntityComponent(GameEntity entity, GameEntity target, ComponentType componentType)
		{
			var componentBoard = Boards.ComponentType.ComponentBoardColumns[(int) componentType.Id];

			var previousComponentId = Boards.Entity.AssignSharedComponent(entity.Id, componentType.Id, target.Id);
			if (previousComponentId > 0)
			{
				var refs = componentBoard.RemoveReference(previousComponentId, entity);

				// nobody reference this component anymore, let's remove the row
				if (refs == 0)
					componentBoard.DeleteRow(previousComponentId);
			}

			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);
		}
		
		/// <summary>
		/// Add multiple component to an entity
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public void AddMultipleComponent(GameEntity entity, Span<ComponentType> componentTypeSpan)
		{
			foreach (ref readonly var componentType in componentTypeSpan)
			{
				var componentBoard = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);
				var cRef           = new ComponentReference(componentType, GameWorldLL.CreateComponent(componentBoard));

				GameWorldLL.AssignComponent(componentBoard, cRef, Boards.Entity, entity);
				GameWorldLL.SetOwner(componentBoard, cRef, entity);
			}

			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);
		}

		/// <summary>
		/// Add a component to an entity
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public ComponentReference AddComponent(GameEntity entity, ComponentType componentType)
		{
			var componentBoard = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);
			var cRef           = new ComponentReference(componentType, GameWorldLL.CreateComponent(componentBoard));

			GameWorldLL.AssignComponent(componentBoard, cRef, Boards.Entity, entity);
			GameWorldLL.SetOwner(componentBoard, cRef, entity);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);

			return cRef;
		}

		/// <summary>
		/// Add a component to an entity
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="data"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ComponentReference AddComponent<T>(GameEntity entity, in T data = default)
			where T : struct, IComponentData
		{
			var componentType = GetComponentType<T>();
			var cRef          = AddComponent(entity, componentType);
			GetComponentData<T>(entity) = data;

			return cRef;
		}

		/// <summary>
		/// Update a component of an entity if it is owned, or create an owned component.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public ComponentReference UpdateOwnedComponent(GameEntity entity, ComponentType componentType)
		{
			var componentMetadata = Boards.Entity.GetComponentColumn(componentType.Id)[(int) entity.Id];
			var componentBoard    = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);

			if (!componentMetadata.IsShared)
			{
				var currentOwner = componentBoard.OwnerColumn[(int) componentMetadata.Id];
				if (currentOwner.Id == entity.Id)
					return new ComponentReference(componentType, componentMetadata.Id);
			}

			// Same signature as AddComponent, but inlined for max performance
			var cRef = new ComponentReference(componentType, GameWorldLL.CreateComponent(componentBoard));
			GameWorldLL.AssignComponent(componentBoard, cRef, Boards.Entity, entity);
			GameWorldLL.SetOwner(componentBoard, cRef, entity);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);

			return cRef;
		}

		/// <summary>
		/// Update a component of an entity if it is owned, or create an owned component.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="value"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ComponentReference UpdateOwnedComponent<T>(GameEntity entity, T value = default)
			where T : struct, IComponentData
		{
			var componentType = GetComponentType<T>();
			var linkColumn    = Boards.Entity.GetComponentColumn(componentType.Id);

			var componentMetadata = linkColumn[(int) entity.Id];
			var componentColumn = Boards.ComponentType
			                            .ComponentBoardColumns[(int) componentType.Id];

			if (!(componentColumn is SingleComponentBoard componentBoard))
				throw new InvalidOperationException();

			if (!componentMetadata.IsShared)
			{
				var currentOwner = componentBoard.OwnerColumn[(int) componentMetadata.Id];
				if (currentOwner.Id == entity.Id)
				{
					componentBoard.SetValue(componentMetadata.Id, value);
					return new ComponentReference(componentType, componentMetadata.Id);
				}
			}

			return AddComponent(entity, value);
		}
	}
}