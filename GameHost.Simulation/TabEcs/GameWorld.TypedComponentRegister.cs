using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using GameHost.Simulation.TabEcs.Types;

namespace GameHost.Simulation.TabEcs
{
    public partial class GameWorld
    {
        private static class TypedComponentRegister
        {
            private static readonly Dictionary<Type, Action<int>> typeNewWorldMap = new();
            private static readonly Dictionary<Type, Action<int>> typeRemoveWorldMap = new();
            private static readonly Dictionary<Type, Action<int, ComponentType>> typeNewComponentMap = new();
            private static readonly Dictionary<Type, Func<int, ComponentType>> typeGetComponentMap = new();

            private static int maxWorldId;

            private static readonly object _Synchronization = new();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void AddWorld(GameWorld world)
            {
                lock (_Synchronization)
                {
                    foreach (var action in typeNewWorldMap.Values)
                        action(world.WorldId);

                    maxWorldId = Math.Max(maxWorldId, world.WorldId);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void RemoveWorld(GameWorld world)
            {
                lock (_Synchronization)
                {
                    foreach (var action in typeRemoveWorldMap.Values)
                        action(world.WorldId);
                }
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

            public static void RegisterType(Type type, Action<int> onNewWorld, Action<int> onRemoveWorld,
                Action<int, ComponentType> onNewComponentType, Func<int, ComponentType> getComponentType)
            {
                lock (_Synchronization)
                {
                    var had = typeNewComponentMap.ContainsKey(type);

                    typeNewWorldMap[type] = onNewWorld;
                    typeRemoveWorldMap[type] = onRemoveWorld;
                    typeNewComponentMap[type] = onNewComponentType;
                    typeGetComponentMap[type] = getComponentType;

                    if (!had) onNewWorld(maxWorldId);
                }
            }

            public static void RemoveType(Type type)
            {
                lock (_Synchronization)
                {
                    typeNewWorldMap.Remove(type);
                    typeNewComponentMap.Remove(type);
                    typeGetComponentMap.Remove(type);
                }
            }
        }

        private static class TypedComponent<T>
        {
            public static ComponentType[] MappedComponentType = Array.Empty<ComponentType>();

            private static readonly object _Synchronization = new();

            static TypedComponent()
            {
                TypedComponentRegister.RegisterType(typeof(T),
                    world =>
                    {
                        lock (_Synchronization)
                        {
                            if (world < MappedComponentType.Length)
                                return;

                            Array.Resize(ref MappedComponentType, world + 1);
                        }
                    },
                    world => { MappedComponentType[world] = default; },
                    (world, ct) => MappedComponentType[world] = ct,
                    world => MappedComponentType[world]);

#if NET
                // We need to remove ourselves when this assembly get unloaded, so that GC can collect this static type.
                void remove(AssemblyLoadContext ctx)
                {
                    ctx.Unloading -= remove;
                    Remove();
                }

                AssemblyLoadContext.GetLoadContext(typeof(T).Assembly).Unloading += remove;
#endif
            }

            // This function does nothing but call the static function
            public static void Register()
            {
            }

            // Remove instances of this type in the register.
            // We keep mappedComponentType in case this was an error or couldn't be unloaded...
            public static void Remove()
            {
                TypedComponentRegister.RemoveType(typeof(T));
            }
        }
    }
}