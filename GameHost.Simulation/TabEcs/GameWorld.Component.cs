﻿using System;
using System.Numerics;
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
			return HasComponent(entity, AsComponentType<T>());
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
			var componentType = AsComponentType<T>().Id;
			var board = Boards.ComponentType.ComponentBoardColumns[(int) componentType];
			if (board is TagComponentBoard)
				return ref TagComponentBoard.Default<T>.V;
			
			if (!(board is SingleComponentBoard componentColumn))
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
			var componentType = AsComponentType<T>().Id;
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
	}
}