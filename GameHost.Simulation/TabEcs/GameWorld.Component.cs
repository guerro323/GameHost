using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.TabEcs.LLAPI;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld
	{
		public const int RecursionLimit = 10;

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
			var componentType = AsComponentType<T>();
			return new ComponentReference(componentType, GameWorldLL.CreateComponent(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, componentType)));
		}

		public EntityBoardContainer.ComponentMetadata GetComponentMetadata(GameEntityHandle entityHandle, ComponentType componentType)
		{
			ThrowOnInvalidHandle(entityHandle);

			return Boards.Entity.GetComponentColumn(componentType.Id)[(int) entityHandle.Id];
		}

		public ComponentReference GetComponentReference<T>(GameEntityHandle entityHandle)
			where T : struct, IEntityComponent
		{
			ThrowOnInvalidHandle(entityHandle);

			var componentType = AsComponentType<T>();
			return new ComponentReference(componentType, Boards.Entity.GetComponentColumn(componentType.Id)[(int) entityHandle.Id].Id);
		}

		public GameEntityHandle GetComponentOwner(ComponentReference component)
		{
			return GameWorldLL.GetOwner(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, component.Type), component);
		}

		public Span<GameEntityHandle> GetReferencedEntities(ComponentReference component)
		{
			return GameWorldLL.GetReferences(GameWorldLL.GetComponentBoardBase(Boards.ComponentType, component.Type), component);
		}

		/// <summary>
		/// Check whether or not an entity has a component
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <param name="componentType"></param>
		/// <returns></returns>
		public bool HasComponent(GameEntityHandle entityHandle, ComponentType componentType)
		{
			var recursionLeft  = RecursionLimit;
			var originalEntity = entityHandle;
			while (recursionLeft-- > 0)
			{
				var link = Boards.Entity.GetComponentColumn(componentType.Id)[(int) entityHandle.Id];
				if (link.IsShared)
				{
					entityHandle = new GameEntityHandle(link.Entity);
					continue;
				}

				return link.Id > 0;
			}

			throw new InvalidOperationException($"HasComponent - Recursion limit reached with '{originalEntity}' and component (backing: {componentType})");
		}

		/// <summary>
		/// Check whether or not an entity has a component
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public bool HasComponent<T>(GameEntityHandle entityHandle)
			where T : struct, IEntityComponent
		{
			return HasComponent(entityHandle, AsComponentType<T>());
		}

		public void GetComponentOf<TList>(GameEntityHandle entityHandle, ComponentType baseType, TList list)
			where TList : IList<ComponentReference>
		{
			ThrowOnInvalidHandle(entityHandle);

			var archetype = GetArchetype(entityHandle);
			foreach (var componentTypeId in Boards.Archetype.GetComponentTypes(archetype.Id))
			{
				if (Boards.ComponentType.ParentTypeColumns[(int) componentTypeId] != baseType)
					continue;

				var componentType = new ComponentType(componentTypeId);
				var metadata      = GetComponentMetadata(entityHandle, componentType);
				if (metadata.Null)
					continue;

				list.Add(new ComponentReference(componentType, metadata.Id));
			}
		}

		/// <summary>
		/// Get the reference to a component data from an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public ref T GetComponentData<T>(GameEntityHandle entityHandle)
			where T : struct, IComponentData
		{
			ThrowOnInvalidHandle(entityHandle);

			var componentType = AsComponentType<T>().Id;
			var board         = Boards.ComponentType.ComponentBoardColumns[(int) componentType];
			if (board is TagComponentBoard)
				return ref TagComponentBoard.Default<T>.V;

			if (!(board is SingleComponentBoard componentColumn))
				throw new InvalidOperationException($"A board made from an {nameof(IComponentData)} should be a {nameof(SingleComponentBoard)}");

#if DEBUG
			if (!HasComponent(entityHandle, new ComponentType(componentType)))
			{
				var msg           = $"{Safe(entityHandle)} has no {Boards.ComponentType.NameColumns[(int) componentType]}. Existing:\n";
				var componentList = Boards.Archetype.GetComponentTypes(GetArchetype(entityHandle).Id);
				foreach (var comp in componentList)
				{
					msg += $"  [{comp}] {Boards.ComponentType.NameColumns[(int) comp]}\n";
				}
				
				throw new InvalidOperationException(msg);
			}
#endif

			return ref componentColumn.AsSpan<T>()[Boards.Entity.GetComponentColumn(componentType)[(int) entityHandle.Id].Assigned];
		}

		/// <summary>
		/// Get the reference to a component data from an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public ref T GetComponentData<T>(ComponentReference componentReference)
			where T : struct, IComponentData
		{
			var board         = Boards.ComponentType.ComponentBoardColumns[(int) componentReference.Type.Id];
			if (board is TagComponentBoard)
				return ref TagComponentBoard.Default<T>.V;

			if (!(board is SingleComponentBoard componentColumn))
				throw new InvalidOperationException($"A board made from an {nameof(IComponentData)} should be a {nameof(SingleComponentBoard)}");
			
			return ref componentColumn.AsSpan<T>()[(int) componentReference.Id];
		}

		/// <summary>
		/// Get the reference to a component data from an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public ref T GetComponentData<T>(GameEntityHandle entityHandle, ComponentType baseType)
			where T : struct, IComponentData
		{
			ThrowOnInvalidHandle(entityHandle);

			var componentType = baseType.Id;
			var board         = Boards.ComponentType.ComponentBoardColumns[(int) componentType];
			if (board is TagComponentBoard)
				return ref TagComponentBoard.Default<T>.V;

			if (!(board is SingleComponentBoard componentColumn))
				throw new InvalidOperationException($"A board made from an {nameof(IComponentData)} should be a {nameof(SingleComponentBoard)}");

#if DEBUG
			if (!HasComponent(entityHandle, new ComponentType(componentType)))
			{
				var msg           = $"{Safe(entityHandle)} has no {Boards.ComponentType.NameColumns[(int) componentType]}. Existing:\n";
				var componentList = Boards.Archetype.GetComponentTypes(GetArchetype(entityHandle).Id);
				foreach (var comp in componentList)
				{
					msg += $"  [{comp}] {Boards.ComponentType.NameColumns[(int) comp]}\n";
				}

				throw new InvalidOperationException(msg);
			}
#endif
			
			return ref componentColumn.AsSpan<T>()[Boards.Entity.GetComponentColumn(componentType)[(int) entityHandle.Id].Assigned];
		}

		/// <summary>
		/// Get a component buffer from an entity
		/// </summary>
		/// <param name="entityHandle"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		public ComponentBuffer<T> GetBuffer<T>(GameEntityHandle entityHandle) where T : struct, IComponentBuffer
		{
			ThrowOnInvalidHandle(entityHandle);

			var componentType = AsComponentType<T>().Id;
			if (!(Boards.ComponentType.ComponentBoardColumns[(int) componentType] is BufferComponentBoard componentColumn))
				throw new InvalidOperationException($"A board made from an {nameof(IComponentBuffer)} should be a {nameof(BufferComponentBoard)}");

#if DEBUG
			if (!HasComponent(entityHandle, new ComponentType(componentType)))
			{
				throw new InvalidOperationException($"{Safe(entityHandle)} has no {Boards.ComponentType.NameColumns[(int) componentType]}.");
			}
#endif

			var recursionLeft  = RecursionLimit;
			var originalEntity = entityHandle;
			while (recursionLeft-- > 0)
			{
				var link = Boards.Entity.GetComponentColumn(componentType)[(int) entityHandle.Id];
				if (link.IsShared)
				{
					entityHandle = new GameEntityHandle(link.Entity);
					continue;
				}

				return new ComponentBuffer<T>(componentColumn.AsSpan()[(int) link.Id]);
			}

			throw new InvalidOperationException($"GetBuffer - Recursion limit reached with '{originalEntity}' and component <{typeof(T)}> (backing: {componentType})");
		}
	}
}