using System;
using System.Collections.Generic;
using System.Diagnostics;
using DefaultEcs;
using GameHost.Entities;
using NetFabric.Hyperlinq;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public class DefaultEcsImplementation
    {
        public readonly RevolutionWorld RevolutionWorld;
        public readonly World           DefaultEcsWorld;

        private List<IDisposable> toDispose = new List<IDisposable>();
        private HashSet<Type> subscribedTypes = new HashSet<Type>();
        
        public DefaultEcsImplementation(RevolutionWorld revolutionWorld, World defaultEcsWorld)
        {
            this.RevolutionWorld = revolutionWorld;
            this.DefaultEcsWorld = defaultEcsWorld;
            
            foreach (var entity in DefaultEcsWorld)
                OnEntityCreated(entity);

            toDispose.Add(DefaultEcsWorld.SubscribeEntityCreated(OnEntityCreated));
            toDispose.Add(DefaultEcsWorld.SubscribeEntityDisposed(OnEntityDisposed));
        }

        private void OnEntityCreated(in Entity entity)
        {
            entity.Set(RevolutionWorld.CreateIdentEntity(entity));
        }

        private void OnEntityDisposed(in Entity entity)
        {
            RevolutionWorld.RemoveEntity(RevolutionWorld.GetEntityFromIdentifier(entity).Raw);
        }

        public void SubscribeComponent<T>()
        {
            if (subscribedTypes.Contains(typeof(T)))
                return;

            // if there is any component, add them to the revolution world
            if (DefaultEcsWorld.Get<T>().Any())
            {
                foreach (var chunk in RevolutionWorld.Chunks)
                {
                    var copy = chunk.Span.ToArray();
                    foreach (var revEnt in copy)
                    {
                        if (!RevolutionWorld.TryGetIdentifier(revEnt, out Entity defaultEntity))
                            continue;

                        if (defaultEntity.Has<T>())
                        {
                            OnComponentAdded(defaultEntity, defaultEntity.Get<T>());
                        }
                    }
                }
            }

            toDispose.Add(DefaultEcsWorld.SubscribeComponentAdded<T>(OnComponentAdded));
            toDispose.Add(DefaultEcsWorld.SubscribeComponentChanged<T>(OnComponentChanged));
            toDispose.Add(DefaultEcsWorld.SubscribeComponentRemoved<T>(OnComponentRemoved));

            subscribedTypes.Add(typeof(T));
        }

        private void OnComponentAdded<T>(in Entity entity, in T component)
        {
            RevolutionWorld.SetComponent(entity.Get<RevolutionEntity>().Raw, component);
        }

        private void OnComponentChanged<T>(in Entity entity, in T previous, in T next)
        {
            RevolutionWorld.SetComponent(entity.Get<RevolutionEntity>().Raw, next);
        }

        private void OnComponentRemoved<T>(in Entity entity, in T component)
        {
            RevolutionWorld.RemoveComponent(entity.Get<RevolutionEntity>().Raw, component.GetType());
        }
    }

    public static class DefaultEcsImplementationExtensions
    {
        public static DefaultEcsImplementation ImplementDefaultEcs(this RevolutionWorld origin, World destination)
        {
            return new DefaultEcsImplementation(origin, destination);
        }
    }
}
