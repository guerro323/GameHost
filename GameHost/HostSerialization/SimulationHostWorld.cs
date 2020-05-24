using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public class PresentationWorld
    {
        public readonly World World;

        internal Dictionary<RawEntity, Entity> entityMapping;

        public PresentationWorld(World world)
        {
            this.World = world;

            entityMapping = new Dictionary<RawEntity, Entity>();
        }

        public static implicit operator World(PresentationWorld t)
        {
            return t.World;
        }

        public Entity GetEntity(RawEntity revolutionEntity)
        {
            lock (World)
            {
                if (entityMapping.TryGetValue(revolutionEntity, out var defaultEcsEntity))
                    return defaultEcsEntity;
            }

            return default(Entity);
        }

        public Entity CreateEntityWithLink(RawEntity revolutionEntity)
        {
            lock (World)
            {
                var defaultEcsEntity = World.CreateEntity();
                defaultEcsEntity.Set(revolutionEntity);
                entityMapping[revolutionEntity] = defaultEcsEntity;

                return defaultEcsEntity;
            }
        }
    }

    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class PresentationHostWorld : AppSystem
    {
        private struct OnUpdateNotification
        {
        }

        [RestrictToApplication(typeof(GameSimulationThreadingHost))]
        private class RestrictedHost : AppSystem
        {
            public readonly RevolutionWorld          RevolutionWorld;
            public readonly DefaultEcsImplementation Implementation;
            public readonly RevolutionWorldAccessor  Accessor;

            public RestrictedHost(WorldCollection collection) : base(collection)
            {
                AddDisposable(RevolutionWorld = new RevolutionWorld());

                Implementation = RevolutionWorld.ImplementDefaultEcs(World.Mgr);
                Accessor       = new DefaultWorldAccessor(RevolutionWorld);
            }

            protected override void OnUpdate()
            {
                World.Mgr.Publish(new OnUpdateNotification());
            }
        }

        private IScheduler     scheduler;
        private RestrictedHost restricted;

        private PresentationWorld                        presentation;
        private Dictionary<Type, ComponentOperationBase> componentsOperation;

        private EntityCommandRecorder recorder;

        public PresentationHostWorld(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref scheduler);
            DependencyResolver.Add(() => ref restricted, new ThreadSystemWithInstanceStrategy<GameSimulationThreadingHost>(Context));

            componentsOperation = new Dictionary<Type, ComponentOperationBase>();
            
            recorder = new EntityCommandRecorder();
            
            presentation = new PresentationWorld(new World());
            
            Context.Bind(presentation);
        }

        protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            // subscribe is thread safe
            AddDisposable(restricted.World.Mgr.Subscribe((in OnUpdateNotification n) =>
            {
                lock (Synchronization)
                {
                    foreach (var chunk in restricted.RevolutionWorld.Chunks)
                    {
                        // todo: make it parallel?
                        foreach (ref readonly var entity in chunk.Span)
                        {
                            var defaultEcsEntity = presentation.GetEntity(entity);
                            if (defaultEcsEntity == default)
                            {
                                defaultEcsEntity = presentation.CreateEntityWithLink(entity);
                            }

                            var record = recorder.Record(defaultEcsEntity);

                            var revEnt = new RevolutionEntity(restricted.RevolutionWorld.Accessor, entity);
                            foreach (var op in componentsOperation.Values)
                            {
                                op.UpdateEntity(defaultEcsEntity, ref record, revEnt);
                            }
                        }
                    }

                    foreach (var (raw, defEnt) in presentation.entityMapping)
                    {
                        if (restricted.RevolutionWorld.Exists(raw))
                            continue;

                        recorder.Record(defEnt)
                                .Dispose();
                    }
                }

                scheduler.AddOnce(Playback);
            }));
        }

        private void Playback()
        {
            lock (Synchronization)
            {
                recorder.Execute(presentation);
            }
            
            scheduler.AddOnce(Playback);
        }

        public void Subscribe<TComponent, TOperation>(TOperation operation)
            where TOperation : ComponentOperationBase<TComponent>
        {
            lock (Synchronization)
            {
                operation.SetPresentation(presentation);
                componentsOperation[typeof(TOperation)] = operation;
            }

            ThreadingHost.GetListener<GameSimulationThreadingHost>()
                         .GetScheduler()
                         .Add(() =>
                         {
                             restricted.Implementation.SubscribeComponent<TComponent>(); 
                         });
        }

        public void Subscribe<TComponent>()
            where TComponent : unmanaged
        {
            Subscribe<TComponent, SimpleComponentOperation<TComponent>>(new SimpleComponentOperation<TComponent>());
        }
    }

    public abstract class ComponentOperationBase
    {
        protected Entity CurrentEntity { get; set; }
        protected PresentationWorld World { get; private set; }

        protected internal virtual void SetPresentation(PresentationWorld presentationWorld)
        {
            World = presentationWorld;
        }

        public abstract void UpdateEntity(in Entity defaultEcsEntity, ref EntityRecord record, in RevolutionEntity revolutionEntity);
        public virtual void OnPlayback() {}
    }

    public abstract class ComponentOperationBase<T> : ComponentOperationBase
    {
        private RevolutionEntity lastEntity;

        public override void UpdateEntity(in Entity defaultEcsEntity, ref EntityRecord record, in RevolutionEntity revEnt)
        {
            CurrentEntity = defaultEcsEntity;
            
            if (revEnt.Chunk.Components.ContainsKey(typeof(T)))
            {
                OnUpdate(ref record, revEnt, revEnt.GetComponent<T>());
            }
            else
            {
                OnRemoved(ref record, revEnt);
            }
        }
        
        protected abstract void OnUpdate(ref EntityRecord record, in RevolutionEntity  revolutionEntity, in T component);
        protected abstract void OnRemoved(ref EntityRecord record, in RevolutionEntity revolutionEntity);
    }

    public class SimpleComponentOperation<T> : ComponentOperationBase<T>
        where T : unmanaged
    {
        protected override void OnUpdate(ref EntityRecord record, in RevolutionEntity revolutionEntity, in T component)
        {
            record.Set(component);
        }

        protected override void OnRemoved(ref EntityRecord record, in RevolutionEntity revolutionEntity)
        {
            record.Remove<T>();
        }
    }
}
