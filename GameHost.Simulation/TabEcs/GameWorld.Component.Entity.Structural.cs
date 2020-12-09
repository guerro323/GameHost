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
		/// <param name="entityHandle">The entity</param>
		/// <param name="componentType">The component type</param>
		/// <returns>True if the component was removed, false if it did not exist.</returns>
		public bool RemoveMultipleComponent(GameEntityHandle entityHandle, Span<ComponentType> componentTypeSpan)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			var b = true;
			foreach (ref readonly var componentType in componentTypeSpan)
			{
				b &= GameWorldLL.RemoveComponentReference(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType), componentType, Boards.Entity, entityHandle);
			}

			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);

			return b;
		}

		/// <summary>
		/// Remove a component from an entity.
		/// </summary>
		/// <param name="entityHandle">The entity</param>
		/// <param name="componentType">The component type</param>
		/// <returns>True if the component was removed, false if it did not exist.</returns>
		public bool RemoveComponent(GameEntityHandle entityHandle, ComponentType componentType)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			if (GameWorldLL.RemoveComponentReference(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType), componentType, Boards.Entity, entityHandle))
			{
				GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);
				return true;
			}

			return false;
		}

		/// <summary>
		/// Assign an existing component to an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="component"></param>
		public void AssignComponent(GameEntityHandle entityHandle, ComponentReference component)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			var board = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, component.Type);
			GameWorldLL.AssignComponent(board, component, Boards.Entity, entityHandle);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);
		}

		/// <summary>
		/// Assign and set as a owner a component to an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="component"></param>
		public void AssignComponentAsOwner(GameEntityHandle entityHandle, ComponentReference component)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			var board = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, component.Type);
			GameWorldLL.AssignComponent(board, component, Boards.Entity, entityHandle);
			GameWorldLL.SetOwner(board, component, entityHandle);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);
		}

		public void DependOnEntityComponent(GameEntityHandle entityHandle, GameEntityHandle target, ComponentType componentType)
		{
			throw new NotImplementedException("This feature is not yet finished.");
			
			/*var componentBoard = Boards.ComponentType.ComponentBoardColumns[(int) componentType.Id];

			var previousComponentId = Boards.Entity.AssignSharedComponent(entity.Id, componentType.Id, target.Id);
			if (previousComponentId > 0)
			{
				var refs = componentBoard.RemoveReference(previousComponentId, entity);

				// nobody reference this component anymore, let's remove the row
				if (refs == 0)
					componentBoard.DeleteRow(previousComponentId);
			}

			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entity);*/
		}
		
		/// <summary>
		/// Add multiple component to an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public void AddMultipleComponent(GameEntityHandle entityHandle, Span<ComponentType> componentTypeSpan)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			foreach (ref readonly var componentType in componentTypeSpan)
			{
				var componentBoard = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);
				var cRef           = new ComponentReference(componentType, GameWorldLL.CreateComponent(componentBoard));

				GameWorldLL.AssignComponent(componentBoard, cRef, Boards.Entity, entityHandle);
				GameWorldLL.SetOwner(componentBoard, cRef, entityHandle);
			}

			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);
		}
		
		/// <summary>
		/// Add and remove multiple component to an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public void AddRemoveMultipleComponent(GameEntityHandle entityHandle, Span<ComponentType> addSpan, Span<ComponentType> removeSpan)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			foreach (ref readonly var componentType in addSpan)
			{
				var componentBoard = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);
				var cRef           = new ComponentReference(componentType, GameWorldLL.CreateComponent(componentBoard));

				GameWorldLL.AssignComponent(componentBoard, cRef, Boards.Entity, entityHandle);
				GameWorldLL.SetOwner(componentBoard, cRef, entityHandle);
			}
			
			foreach (ref readonly var componentType in removeSpan)
			{
				GameWorldLL.RemoveComponentReference(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType), componentType, Boards.Entity, entityHandle);
			}

			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);
		}

		/// <summary>
		/// Add a component to an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public ComponentReference AddComponent(GameEntityHandle entityHandle, ComponentType componentType)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			var componentBoard = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);
			var cRef           = new ComponentReference(componentType, GameWorldLL.CreateComponent(componentBoard));

			GameWorldLL.AssignComponent(componentBoard, cRef, Boards.Entity, entityHandle);
			GameWorldLL.SetOwner(componentBoard, cRef, entityHandle);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);

			return cRef;
		}

		/// <summary>
		/// Add a component to an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="data"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ComponentReference AddComponent<T>(GameEntityHandle entityHandle, in T data = default)
			where T : struct, IComponentData
		{
			var componentType = AsComponentType<T>();
			var cRef          = AddComponent(entityHandle, componentType);
			GetComponentData<T>(entityHandle) = data;

			return cRef;
		}
		
		/// <summary>
		/// Add a component to an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ComponentBuffer<T> AddBuffer<T>(GameEntityHandle entityHandle)
			where T : struct, IComponentBuffer
		{
			var componentType = AsComponentType<T>();
			AddComponent(entityHandle, componentType);

			return GetBuffer<T>(entityHandle);
		}

		/// <summary>
		/// Update a component of an entity if it is owned, or create an owned component.
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public ComponentReference UpdateOwnedComponent(GameEntityHandle entityHandle, ComponentType componentType)
		{
			ThrowOnInvalidHandle(entityHandle);
			
			var componentMetadata = Boards.Entity.GetComponentColumn(componentType.Id)[(int) entityHandle.Id];
			var componentBoard    = GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType);

			if (!componentMetadata.IsShared)
			{
				var currentOwner = componentBoard.OwnerColumn[(int) componentMetadata.Id];
				if (currentOwner.Id == entityHandle.Id)
					return new ComponentReference(componentType, componentMetadata.Id);
			}

			// Same signature as AddComponent, but inlined for max performance
			var cRef = new ComponentReference(componentType, GameWorldLL.CreateComponent(componentBoard));
			GameWorldLL.AssignComponent(componentBoard, cRef, Boards.Entity, entityHandle);
			GameWorldLL.SetOwner(componentBoard, cRef, entityHandle);
			GameWorldLL.UpdateArchetype(Boards.Archetype, Boards.ComponentType, Boards.Entity, entityHandle);

			return cRef;
		}

		/// <summary>
		/// Update a component of an entity if it is owned, or create an owned component.
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="value"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public ComponentReference UpdateOwnedComponent<T>(GameEntityHandle entityHandle, T value = default)
			where T : struct, IComponentData
		{
			ThrowOnInvalidHandle(entityHandle);
			
			var componentType = AsComponentType<T>();
			var linkColumn    = Boards.Entity.GetComponentColumn(componentType.Id);

			var componentMetadata = linkColumn[(int) entityHandle.Id];
			var componentColumn = Boards.ComponentType
			                            .ComponentBoardColumns[(int) componentType.Id];

			if (!(componentColumn is SingleComponentBoard componentBoard))
				throw new InvalidOperationException();

			if (!componentMetadata.IsShared && componentMetadata.Valid)
			{
				var currentOwner = componentBoard.OwnerColumn[(int) componentMetadata.Id];
				if (currentOwner.Id == entityHandle.Id)
				{
					componentBoard.SetValue(componentMetadata.Id, value);
					return new ComponentReference(componentType, componentMetadata.Id);
				}
			}

			return AddComponent(entityHandle, value);
		}
	}
}