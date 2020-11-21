using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using GameHost.Simulation.TabEcs.Interfaces;
using NetFabric.Hyperlinq;
using StormiumTeam.GameBase.Utility.Misc;
using Array = System.Array;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld : IDisposable
	{
		private static int s_WorldIdCounter = 1;

		public struct __Boards
		{
			public EntityBoardContainer        Entity;
			public ArchetypeBoardContainer     Archetype;
			public ComponentTypeBoardContainer ComponentType;
		}

		public readonly __Boards Boards;
		public readonly int      WorldId;

		public GameWorld(__Boards baseBoards = default)
		{
			WorldId = s_WorldIdCounter++;

			TypedComponentRegister.AddWorld(this);

			Boards = new __Boards
			{
				Entity        = baseBoards.Entity ?? new EntityBoardContainer(0),
				Archetype     = baseBoards.Archetype ?? new ArchetypeBoardContainer(0),
				ComponentType = baseBoards.ComponentType ?? new ComponentTypeBoardContainer(0)
			};
		}

		public ComponentType RegisterComponent(string name, ComponentBoardBase componentBoard, Type optionalManagedType = null)
		{
			if (Boards.ComponentType.Registered.Where(row => Boards.ComponentType.NameColumns[(int) row.Id] == name).Count() > 0)
				throw new InvalidOperationException($"A component named '{name}' already exist");

			return new ComponentType(Boards.ComponentType.CreateRow(name, componentBoard));
		}

		public ComponentType AsComponentType(Type type)
		{
			var componentType = TypedComponentRegister.GetComponentType(WorldId, type);
			if (componentType.Id > 0)
				return componentType;

			var method = typeof(GameWorld).GetMethods()
			                              .Single(m => m.Name == nameof(AsComponentType) && m.IsGenericMethodDefinition);

			return (ComponentType) method.MakeGenericMethod(type).Invoke(this, null);
		}

		public ComponentType AsComponentType<T>()
			where T : struct, IEntityComponent
		{
			var componentType = TypedComponent<T>.MappedComponentType[WorldId];
			if (componentType.Id > 0)
				return componentType;

			ComponentBoardBase board = null;
			if (typeof(IComponentData).IsAssignableFrom(typeof(T)))
			{
				if (ComponentTypeUtility.IsZeroSizeStruct(typeof(T)))
				{
					board = new TagComponentBoard(0);
				}
				else
				{
					board = new SingleComponentBoard(Unsafe.SizeOf<T>(), 0);
				}
			}
			else if (typeof(IComponentBuffer).IsAssignableFrom(typeof(T)))
				board = new BufferComponentBoard(Unsafe.SizeOf<T>(), 0);
			else
				throw new InvalidOperationException();

			componentType = RegisterComponent(TypeExt.GetFriendlyName(typeof(T)), board);
			TypedComponentRegister.AddComponent(WorldId, typeof(T), componentType);
			return componentType;
		}

		public TBoard GetComponentBoard<TBoard>(ComponentType componentType)
			where TBoard : ComponentBoardBase
		{
			return (TBoard) Boards.ComponentType.ComponentBoardColumns[(int) componentType.Id];
		}

		public void Dispose()
		{
			Boards.Entity.Dispose();
			Boards.Archetype.Dispose();
			Boards.ComponentType.Dispose();

			TypedComponentRegister.RemoveWorld(this);
		}
	}
}