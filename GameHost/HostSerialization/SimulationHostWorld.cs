using System;
using System.Collections.Generic;
using System.Diagnostics;
using Collections.Pooled;
using DefaultEcs;
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

        public PresentationWorld(World world)
        {
            this.World = world;
        }

        public static implicit operator World(PresentationWorld t)
        {
            return t.World;
        }
    }
    
    public class PresentationRevolutionWorld : RevolutionWorld
    {
        public PresentationRevolutionWorld(int capacity) : base(capacity)
        {
        }
    }
    
    [RestrictToApplication(typeof(GameRenderThreadingHost))]
    public class PresentationHostWorld : AppSystem
    {
        private struct OnUpdateNotification
        {
        }

        [RestrictToApplication(typeof(GameSimulationThreadingHost))]
        public class RestrictedHost : AppSystem
        {
            public readonly RevolutionWorld RevolutionWorld;
            public readonly DefaultEcsImplementation Implementation;
            public readonly RevolutionWorldAccessor Accessor;

            public RestrictedHost(WorldCollection collection) : base(collection)
            {
                AddDisposable(RevolutionWorld = new PresentationRevolutionWorld(128));

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

        private RevolutionWorldAccessor accessor;
        private PresentationWorld presentation;

        public PresentationHostWorld(WorldCollection collection) : base(collection)
        {
            FrameWorlds                 = new PooledList<RevolutionWorld>();
            revolutionWorldOnSimulation = new RevolutionWorld();

            presentation = new PresentationWorld(new World());
            
            DependencyResolver.Add(() => ref scheduler);
            DependencyResolver.Add(() => ref restricted, new ThreadSystemWithInstanceStrategy<GameSimulationThreadingHost>(Context));
        }

        private void invalidateActiveWorlds()
        {
            areInvalidated = true;
        }

        protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            lock (restricted.Synchronization)
            {
                accessor = restricted.Accessor;
            }

            // subscribe is thread safe
            AddDisposable(restricted.World.Mgr.Subscribe((in OnUpdateNotification n) =>
            {
                var clonedWorld = restricted.RevolutionWorld.Clone();

                void clearWorldQueue()
                {
                    foreach (var world in FrameWorlds)
                        world.Dispose();
                    FrameWorlds.Clear();
                }

                void addWorldToQueue()
                {
                    areInvalidated = false;
                    FrameWorlds.Add(clonedWorld);
                    
                    
                }

                scheduler.AddOnce(invalidateActiveWorlds);
                scheduler.AddOnce(clearWorldQueue);
                scheduler.Add(addWorldToQueue);
            }));
        }

        /// <summary>
        /// This revolution world will be used in the simulation application
        /// </summary>
        private readonly RevolutionWorld revolutionWorldOnSimulation;

        /// <summary>
        /// Get a list of frame worlds
        /// </summary>
        public readonly PooledList<RevolutionWorld> FrameWorlds;

        /// <summary>
        /// The latest world
        /// </summary>
        public RevolutionWorld LastWorld => FrameWorlds.Count > 0 ? FrameWorlds[^1] : null;

        private bool areInvalidated;

        public ReadOnlySpan<RevolutionWorld> ActiveWorlds
        {
            get
            {
                return FrameWorlds.Span.Slice(0, areInvalidated ? 0 : FrameWorlds.Count);
            }
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            scheduler.AddOnce(invalidateActiveWorlds);
        }

        public RevolutionEntity ToNormalized(RawEntity entity)
        {
            return new RevolutionEntity(accessor, entity);
        }
    }
}
