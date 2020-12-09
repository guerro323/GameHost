using System;
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
			ThrowOnInvalidHandle(entityHandle);
			
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
			var board = Boards.ComponentType.ComponentBoardColumns[(int) componentType];
			if (board is TagComponentBoard)
				return ref TagComponentBoard.Default<T>.V;
			
			if (!(board is SingleComponentBoard componentColumn))
				throw new InvalidOperationException($"A board made from an {nameof(IComponentData)} should be a {nameof(SingleComponentBoard)}");

			return ref componentColumn.AsSpan<T>()[Boards.Entity.GetComponentColumn(componentType)[(int) entityHandle.Id].Assigned];
			
			/*var recursionLeft  = RecursionLimit;
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

			throw new InvalidOperationException($"GetComponentData - Recursion limit reached with '{originalEntity}' and component <{typeof(T)}> (backing: {componentType})");*/
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