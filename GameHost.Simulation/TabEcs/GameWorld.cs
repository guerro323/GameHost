using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using GameHost.Simulation.TabEcs.Interfaces;
using NetFabric.Hyperlinq;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld : IDisposable
	{
		private Dictionary<Type, ComponentType> typeToComponentMap;

		public struct __Boards
		{
			public EntityBoardContainer        Entity;
			public ArchetypeBoardContainer     Archetype;
			public ComponentTypeBoardContainer ComponentType;
		}

		public readonly __Boards Boards;

		public GameWorld()
		{
			typeToComponentMap = new Dictionary<Type, ComponentType>();

			Boards = new __Boards
			{
				Entity        = new EntityBoardContainer(0),
				Archetype     = new ArchetypeBoardContainer(0),
				ComponentType = new ComponentTypeBoardContainer(0)
			};
		}

		public ComponentType RegisterComponent(string name, ComponentBoardBase componentBoard)
		{
			if (Boards.ComponentType.Registered.Where(row => Boards.ComponentType.NameColumns[(int) row.Id] == name).Count() > 0)
				throw new InvalidOperationException($"A component named '{name}' already exist");

			return new ComponentType(Boards.ComponentType.CreateRow(name, componentBoard));
		}

		public ComponentType GetComponentType<T>()
			where T : struct, IEntityComponent
		{
			if (typeToComponentMap.TryGetValue(typeof(T), out var componentType))
				return componentType;

			ComponentBoardBase board = null;
			if (typeof(IComponentData).IsAssignableFrom(typeof(T)))
				board = new SingleComponentBoard(Unsafe.SizeOf<T>(), 0);
			else if (typeof(IComponentBuffer).IsAssignableFrom(typeof(T)))
				board = new BufferComponentBoard(Unsafe.SizeOf<T>(), 0);
			else
				throw new InvalidOperationException();
			
			return typeToComponentMap[typeof(T)] = RegisterComponent(typeof(T).FullName, board);
		}

		public void Dispose()
		{
		}
	}
}