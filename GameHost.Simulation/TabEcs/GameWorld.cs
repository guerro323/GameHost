using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GameHost.Simulation.TabEcs.Interfaces;
using NetFabric.Hyperlinq;
using StormiumTeam.GameBase.Utility.Misc;
using Array = System.Array;

namespace GameHost.Simulation.TabEcs
{
	public partial class GameWorld : IDisposable
	{
		private static class TypedComponentRegister
		{
			private static Dictionary<Type, Action<int>>                typeNewWorldMap     = new Dictionary<Type, Action<int>>();
			private static Dictionary<Type, Action<int, ComponentType>> typeNewComponentMap = new Dictionary<Type, Action<int, ComponentType>>();
			private static Dictionary<Type, Func<int, ComponentType>> typeGetComponentMap = new Dictionary<Type, Func<int, ComponentType>>();

			private static int maxWorldId;
			
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void AddWorld(GameWorld world)
			{
				foreach (var action in typeNewWorldMap.Values)
					action(world.WorldId);

				maxWorldId = Math.Max(maxWorldId, world.WorldId);
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public static void AddComponent(int worldId, Type original, ComponentType componentType)
			{
				typeNewComponentMap[original](worldId, componentType);
			}

			public static ComponentType GetComponentType(int worldId, Type original)
			{
				if (!typeGetComponentMap.ContainsKey(original))
					return default;
				return typeGetComponentMap[original](worldId);
			}

			public static void RegisterType(Type type, Action<int> onNewWorld, Action<int, ComponentType> onNewComponentType, Func<int, ComponentType> getComponentType)
			{
				var had = typeNewComponentMap.ContainsKey(type);
				
				typeNewWorldMap[type]     = onNewWorld;
				typeNewComponentMap[type] = onNewComponentType;
				typeGetComponentMap[type] = getComponentType;

				if (!had)
				{
					onNewWorld(maxWorldId);
				}
			}
		}

		private static class TypedComponent<T>
		{
			public static ComponentType[] mappedComponentType = Array.Empty<ComponentType>();

			static TypedComponent()
			{
				TypedComponentRegister.RegisterType(typeof(T),
					world =>
					{
						if (world < mappedComponentType.Length)
							return;
						Array.Resize(ref mappedComponentType, world + 1);
					},
					(world, ct) => mappedComponentType[world] = ct,
					world => mappedComponentType[world]);
			}

			// This function does nothing but call the static function
			public static void Register()
			{
				
			}
		}

		private static int s_WorldIdCounter = 1;
		
		public struct __Boards
		{
			public EntityBoardContainer        Entity;
			public ArchetypeBoardContainer     Archetype;
			public ComponentTypeBoardContainer ComponentType;
		}

		public readonly __Boards Boards;
		public readonly int      WorldId;

		public GameWorld()
		{
			WorldId            = s_WorldIdCounter++;

			TypedComponentRegister.AddWorld(this);
			
			Boards = new __Boards
			{
				Entity        = new EntityBoardContainer(0),
				Archetype     = new ArchetypeBoardContainer(0),
				ComponentType = new ComponentTypeBoardContainer(0)
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
			var componentType = TypedComponent<T>.mappedComponentType[WorldId];
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

		public void Dispose()
		{
		}
	}
}