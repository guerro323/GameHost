﻿using System;
using System.Collections.Generic;
using DefaultEcs;
using DefaultEcs.Serialization;
using NetFabric.Hyperlinq;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public class DefaultEcsImplementation
    {
        public readonly RevolutionWorld RevolutionWorld;
        public readonly World           DefaultEcsWorld;

        private List<IDisposable> toDispose = new List<IDisposable>();

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

        private delegate ref T delegateSet<T>(RawEntity entity, in T comp);

        private class ComponentOperation<T>
        {
            public delegateSet<T> Set;
        }

        private Dictionary<Type, object> operations = new Dictionary<Type, object>();

        public void SubscribeComponent<T>()
            where T : IRevolutionComponent
        {
            operations[typeof(T)] = new ComponentOperation<T> {Set = (delegateSet<T>)typeof(RevolutionWorld).GetMethod("SetComponent").MakeGenericMethod(typeof(T)).CreateDelegate(typeof(delegateSet<T>), RevolutionWorld)};

            // if there is any component, add them to the revolution world
            if (DefaultEcsWorld.Get<T>().Any())
            {
                foreach (var chunk in RevolutionWorld.Chunks)
                {
                    foreach (ref readonly var revEnt in chunk.Span)
                    {
                        if (!RevolutionWorld.TryGetIdentifier(revEnt, out Entity defaultEntity))
                            continue;

                        if (defaultEntity.Has<T>())
                            OnComponentAdded(defaultEntity, defaultEntity.Get<T>());
                    }
                }
            }

            toDispose.Add(DefaultEcsWorld.SubscribeComponentAdded<T>(OnComponentAdded));
            toDispose.Add(DefaultEcsWorld.SubscribeComponentChanged<T>(OnComponentChanged));
            toDispose.Add(DefaultEcsWorld.SubscribeComponentRemoved<T>(OnComponentRemoved));
        }

        private void OnComponentAdded<T>(in Entity entity, in T component)
        {
            if (component is IRevolutionComponent)
                ((ComponentOperation<T>)operations[typeof(T)]).Set(entity.Get<RevolutionEntity>().Raw, component);
        }

        private void OnComponentChanged<T>(in Entity entity, in T previous, in T next)
        {
            if (next is IRevolutionComponent)
                ((ComponentOperation<T>)operations[typeof(T)]).Set(entity.Get<RevolutionEntity>().Raw, next);
        }

        private void OnComponentRemoved<T>(in Entity entity, in T component)
        {
            if (component is IRevolutionComponent)
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
