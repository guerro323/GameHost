using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Collections.Pooled;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.HostSerialization.imp;
using GameHost.Injection;
using RevolutionSnapshot.Core;
using RevolutionSnapshot.Core.ECS;

namespace GameHost.HostSerialization
{
    public class PresentationWorld
    {
        public readonly World World;

        internal Dictionary<RawEntity, Entity> entityMapping;
        internal TwoWayDictionary<Entity, Entity> sourceToConvertedMap;

        public PresentationWorld(World world)
        {
            this.World = world;

            entityMapping = new Dictionary<RawEntity, Entity>();
            sourceToConvertedMap = new TwoWayDictionary<Entity, Entity>();
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

        public Entity CreateEntityWithLink(RevolutionEntity revolutionEntity)
        {
            lock (World)
            {
                var defaultEcsEntity = World.CreateEntity();
                defaultEcsEntity.Set(revolutionEntity);
                entityMapping[revolutionEntity.Raw] = defaultEcsEntity;

                if (revolutionEntity.Accessor is DefaultWorldAccessor accessor
                    && accessor.World.TryGetIdentifier(revolutionEntity.Raw, out Entity entityIdentifier))
                    sourceToConvertedMap.Set(entityIdentifier, defaultEcsEntity);

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

            presentation = new PresentationWorld(World.Mgr);

            Context.Bind(presentation);
        }

        protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            // subscribe is thread safe
            AddDisposable(restricted.World.Mgr.Subscribe((in OnUpdateNotification n) =>
            {
                lock (Synchronization)
                {
                    var sw = new Stopwatch();
                    sw.Start();
                    foreach (var chunk in restricted.RevolutionWorld.Chunks)
                    {
                        // todo: make it parallel?
                        foreach (ref readonly var entity in chunk.Span)
                        {
                            var defaultEcsEntity = presentation.GetEntity(entity);
                            if (defaultEcsEntity == default)
                            {
                                defaultEcsEntity = presentation.CreateEntityWithLink(new RevolutionEntity(restricted.RevolutionWorld.Accessor, entity));
                            }

                            var record = recorder.Record(defaultEcsEntity);

                            var revEnt = new RevolutionEntity(restricted.RevolutionWorld.Accessor, entity);
                            foreach (var op in componentsOperation.Values)
                            {
                                op.UpdateEntity(defaultEcsEntity, ref record, revEnt);
                            }
                        }
                    }

                    Parallel.ForEach(presentation.entityMapping, kvp =>
                    {
                        var (raw, defEnt) = kvp;
                        if (restricted.RevolutionWorld.Exists(raw))
                            return;

                        recorder.Record(defEnt)
                                .Dispose();
                    });
                    sw.Stop();
                }

                scheduler.AddOnce(Playback);
            }));
        }

        private void Playback()
        {
            lock (Synchronization)
            {
                recorder.Execute(presentation);
                foreach (var op in componentsOperation.Values)
                    op.OnPlayback();
            }

            scheduler.AddOnce(Playback);
        }

        public TOperation Subscribe<TOperation>(TOperation operation)
            where TOperation : ComponentOperationBase
        {
            lock (Synchronization)
            {
                operation.SetPresentation(presentation);
                componentsOperation[typeof(TOperation)] = operation;

                operation.ImpAdded += imp =>
                {
                    lock (Synchronization)
                    {
                        if (imp is IGetEntityMap getEntityMap)
                            getEntityMap.SourceToConvertedMap = presentation.sourceToConvertedMap;
                    }
                };
            }

            ThreadingHost.GetListener<GameSimulationThreadingHost>()
                         .GetScheduler()
                         .Add(() =>
                         {
                             operation.Subscribe(restricted.Implementation);
                         });

            return operation;
        }

        public SimpleComponentOperation<TComponent> Subscribe<TComponent>()
            where TComponent : unmanaged
        {
            return Subscribe(new SimpleComponentOperation<TComponent>());
        }
    }

    public abstract class ComponentOperationBase
    {
        public event Action<ImpBase> ImpAdded; 
        
        protected Entity CurrentEntity { get; set; }
        protected PresentationWorld World { get; private set; }

        protected internal virtual void SetPresentation(PresentationWorld presentationWorld)
        {
            World = presentationWorld;
        }

        internal abstract void Subscribe(in DefaultEcsImplementation implementation);
        public abstract void UpdateEntity(in Entity defaultEcsEntity, ref EntityRecord record, in RevolutionEntity revolutionEntity);
        public virtual void OnPlayback() {}

        protected void CallOnImpAdded(ImpBase imp) => ImpAdded?.Invoke(imp);
    }

    public abstract class ComponentOperationBase<T> : ComponentOperationBase
    {
        private RevolutionEntity lastEntity;
        
        private List<ImpBase<T>> imps = new List<ImpBase<T>>();

        public override void UpdateEntity(in Entity defaultEcsEntity, ref EntityRecord record, in RevolutionEntity revEnt)
        {
            CurrentEntity = defaultEcsEntity;

            if (revEnt.Chunk.Components.ContainsKey(typeof(T)))
            {
                var component = revEnt.GetComponent<T>();
                foreach (var imp in imps)
                    imp.OnImp(ref component);
                OnUpdate(ref record, revEnt, component);
            }
            else
            {
                OnRemoved(ref record, revEnt);
            }
        }

        protected abstract void OnUpdate(ref  EntityRecord record, in RevolutionEntity revolutionEntity, in T component);
        protected abstract void OnRemoved(ref EntityRecord record, in RevolutionEntity revolutionEntity);

        internal override void Subscribe(in DefaultEcsImplementation implementation)
        {
            implementation.SubscribeComponent<T>();
        }

        public virtual ComponentOperationBase<T> AddImp<TImp>(TImp imp)
            where TImp : ImpBase<T>
        {
           CallOnImpAdded(imp);

            imps.Add(imp);
            return this;
        }
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
