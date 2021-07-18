using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
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

		public void SwitchStructuralThread()
		{
			var currentThread = Thread.CurrentThread;
			Boards.Entity.SetCallerThread(currentThread);
			Boards.Archetype.SetCallerThread(currentThread);
			Boards.ComponentType.SetCallerThread(currentThread);

			foreach (var board in Boards.ComponentType.ComponentBoardColumns)
				board?.SetCallerThread(currentThread);
		}

		public bool HasComponentType(string name)
		{
			return Boards.ComponentType.Registered.Any(row => Boards.ComponentType.NameColumns[(int) row.Id] == name);
		}

		private object lockedComponentTypeSynchronization = new();

		public ComponentType GetComponentType(string name)
		{
			lock (lockedComponentTypeSynchronization)
			{
				var componentTypeBoard = Boards.ComponentType;
				for (var i = 1; i < componentTypeBoard.Registered.Length; i++)
				{
					if (componentTypeBoard.NameColumns[i] == name)
						return new ComponentType((uint) i);
				}
			}

			throw new KeyNotFoundException(name);
		}

		public ComponentType RegisterComponent(string name, ComponentBoardBase componentBoard, Type optionalManagedType = null, ComponentType optionalParentType = default)
		{
			if (HasComponentType(name))
				throw new InvalidOperationException($"[{WorldId}] A component named '{name}' already exist");

			lock (lockedComponentTypeSynchronization)
				return new ComponentType(Boards.ComponentType.CreateRow(name, componentBoard, optionalParentType: optionalParentType));
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

			lock (lockedComponentTypeSynchronization)
			{
				// This can happen if a thread [A] started creating a component while thread [B] was also going to
				componentType = TypedComponent<T>.MappedComponentType[WorldId];
				if (componentType.Id > 0)
					return componentType;
				
				ComponentBoardBase board = null;
				if (default(T) is IMetadataCustomComponentBoard metadataCustomComponentBoard)
				{
					board = metadataCustomComponentBoard.ProvideComponentBoard(this);
				}
				else if (typeof(IComponentData).IsAssignableFrom(typeof(T)))
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
					// it's not possible to create nor destroy component of this type
					board = new ReadOnlyComponentBoard(0, 0);

				string componentName;
				if (default(T) is IMetadataCustomComponentName metadataCustomComponentName)
				{
					componentName = metadataCustomComponentName.ProvideName(this);
				}
				else
				{
					componentName = TypeExt.GetFriendlyName(typeof(T));
				}

				ComponentType parent = default;
				if (default(T) is IMetadataSubComponentOf metadataSubComponentOf)
				{
					lock (lockedComponentTypeSynchronization)
						parent = metadataSubComponentOf.ProvideComponentParent(this);
				}

				componentType = RegisterComponent(componentName, board, optionalParentType: parent);
				TypedComponentRegister.AddComponent(WorldId, typeof(T), componentType);
			}

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