using System;
using System.Numerics;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.TabEcs.LLAPI;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld
	{
		public const int RecursionLimit = 10;

		public EntityBoardContainer.ComponentMetadata GetComponentMetadata(GameEntity entity, ComponentType componentType)
		{
			return Boards.Entity.GetComponentColumn(componentType.Id)[(int) entity.Id];
		}

		/// <summary>
		/// Check whether or not an entity has a component
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public bool HasComponent(GameEntity entity, ComponentType componentType)
		{
			var recursionLeft  = RecursionLimit;
			var originalEntity = entity;
			while (recursionLeft-- > 0)
			{
				var link = Boards.Entity.GetComponentColumn(componentType.Id)[(int) entity.Id];
				if (link.IsShared)
				{
					entity = new GameEntity(link.Entity);
					continue;
				}

				return link.Id > 0;
			}

			throw new InvalidOperationException($"HasComponent - Recursion limit reached with '{originalEntity}' and component (backing: {componentType})");
		}

		/// <summary>
		/// Check whether or not an entity has a component
		/// </summary>
		/// <param name="entity"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public bool HasComponent<T>(GameEntity entity)
			where T : struct, IComponentData
		{
			return HasComponent(entity, GetComponentType<T>());
		}

		/// <summary>
		/// Get the reference to a component data from an entity
		/// </summary>
		/// <param name="entity"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public ref T GetComponentData<T>(GameEntity entity)
			where T : struct, IComponentData
		{
			var componentType = GetComponentType<T>().Id;
			if (!(Boards.ComponentType.ComponentBoardColumns[(int) componentType] is SingleComponentBoard componentColumn))
				throw new InvalidOperationException($"A board made from an {nameof(IComponentData)} should be a {nameof(SingleComponentBoard)}");

			var recursionLeft  = RecursionLimit;
			var originalEntity = entity;
			while (recursionLeft-- > 0)
			{
				var link = Boards.Entity.GetComponentColumn(componentType)[(int) entity.Id];
				if (link.IsShared)
				{
					entity = new GameEntity(link.Entity);
					continue;
				}

				return ref componentColumn.Read<T>(link.Id);
			}

			throw new InvalidOperationException($"GetComponentData - Recursion limit reached with '{originalEntity}' and component <{typeof(T)}> (backing: {componentType})");
		}

		/// <summary>
		/// Get a component buffer from an entity
		/// </summary>
		/// <param name="entity"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public ComponentBuffer<T> GetBuffer<T>(GameEntity entity) where T : struct, IComponentBuffer
		{
			var componentType = GetComponentType<T>().Id;
			if (!(Boards.ComponentType.ComponentBoardColumns[(int) componentType] is BufferComponentBoard componentColumn))
				throw new InvalidOperationException($"A board made from an {nameof(IComponentBuffer)} should be a {nameof(BufferComponentBoard)}");

			var recursionLeft  = RecursionLimit;
			var originalEntity = entity;
			while (recursionLeft-- > 0)
			{
				var link = Boards.Entity.GetComponentColumn(componentType)[(int) entity.Id];
				if (link.IsShared)
				{
					entity = new GameEntity(link.Entity);
					continue;
				}

				return new ComponentBuffer<T>(componentColumn.AsSpan()[(int) link.Id]);
			}

			throw new InvalidOperationException($"GetBuffer - Recursion limit reached with '{originalEntity}' and component <{typeof(T)}> (backing: {componentType})");
		}

		/// <summary>
		/// Remove a component from an entity.
		/// </summary>
		/// <param name="entity">The entity</param>
		/// <param name="componentType">The component type</param>
		/// <returns>True if the component was removed, false if it did not exist.</returns>
		public bool RemoveComponent(GameEntity entity, ComponentType componentType)
		{
			return GameWorldLL.RemoveComponentReference(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType), componentType, Boards.Entity, entity);
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
		}

		/// <summary>
		/// Create a new component
		/// </summary>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public ComponentReference CreateComponent(ComponentType componentType)
		{
			return new ComponentReference(componentType, GameWorldLL.CreateComponent(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType)));
		}

		/// <summary>
		/// Create a new component
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ComponentReference CreateComponent<T>()
			where T : struct, IEntityComponent
		{
			var componentType = GetComponentType<T>();
			return new ComponentReference(componentType, GameWorldLL.CreateComponent(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType)));
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
			var componentBoard = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);

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