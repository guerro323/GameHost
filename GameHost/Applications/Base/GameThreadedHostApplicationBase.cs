using System;
using System.Collections.Generic;
using System.Diagnostics;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Game;
using GameHost.Core.Threading;
using GameHost.Entities;
using GameHost.Injection;

namespace GameHost.Applications
{
    public abstract class GameThreadedHostApplicationBase<T> : ThreadingHost<T>
    {
        private TimeSpan frequency;

        protected Dictionary<Instance, WorldCollection> MappedWorldCollection = new Dictionary<Instance, WorldCollection>(1);
        protected List<Type>                            queuedSystemTypes     = new List<Type>();

        protected List<Type> systemTypes = new List<Type>();

        protected ApplicationWorker Worker { get; }

        protected GameThreadedHostApplicationBase(Context context, TimeSpan? frequency = null)
        {
            this.frequency = frequency ?? TimeSpan.FromSeconds(1f / 1000f);
            Context        = context;
            Worker         = new ApplicationWorker(GetType().Name);
        }

        protected bool    QuitApplication { get; set; }
        protected Context Context         { get; }

        protected virtual bool autoResolveSystem => true;

        public TimeSpan Frequency
        {
            get
            {
                using (SynchronizeThread())
                {
                    return frequency;
                }
            }
            set => ThreadingHost.Synchronize<GameSimulationThreadingHost, TimeSpan, TimeSpan>(f => frequency = f, value, null, value, cc: CancellationToken);
        }

        protected override void OnThreadStart()
        {
            // Automatically add systems when asked to auto resolve them...
            if (autoResolveSystem) AppSystemResolver.ResolveFor<T>(queuedSystemTypes);

            OnInit();

            var elapsedTime = TimeSpan.Zero;
            var fts         = new FixedTimeStep {TargetFrameTimeMs = Frequency.Milliseconds};
            while (!CancellationToken.IsCancellationRequested && !QuitApplication)
            {
                // We ask for the scheduler to run the tasks it was asked to in the beginning of this frame.
                // TODO: Or maybe we should do that in the end of the frame?
                GetScheduler().Run();

                // Try add queued systems into other worlds...
                if (queuedSystemTypes.Count > 0)
                {
                    // Make sure no one can mess with thread safety here
                    using (SynchronizeThread())
                    {
                        OnQueuedSystemsAdded();
                    }

                    systemTypes.AddRange(queuedSystemTypes);
                    queuedSystemTypes.Clear();
                }

                var delta       = Worker.Delta;
                var updateCount = fts.GetUpdateCount(delta.TotalSeconds);

                using (Worker.StartMonitoring(Frequency))
                {
                    // Make sure no one can mess with thread safety here
                    using (SynchronizeThread())
                        OnUpdate(ref updateCount, elapsedTime);
                }

                var wait = frequency - delta;
                if (wait > TimeSpan.Zero)
                {
                    CancellationToken.WaitHandle.WaitOne(wait);
                }
            }

            OnQuit();
        }

        protected abstract void OnInit();

        protected virtual void OnUpdate(ref int fixedUpdateCount, TimeSpan elapsedTime)
        {
            for (var i = 0; i != fixedUpdateCount; i++)
            {
                OnFixedUpdate(i, frequency, elapsedTime);
            }
        }

        protected virtual void OnFixedUpdate(int step, TimeSpan delta, TimeSpan elapsedTime)
        {
            foreach (var world in MappedWorldCollection.Values)
            {
                world.DoInitializePass();
                var timeSystem = world.GetOrCreate(collection => new TimeSystem {WorldCollection = collection});
                timeSystem.Update(new WorldTime {Delta = delta, Total = elapsedTime});
                world.DoUpdatePass();
            }
        }

        protected abstract void OnQuit();

        protected virtual void OnQueuedSystemsAdded()
        {
            foreach (var world in MappedWorldCollection.Values)
            {
                foreach (var systemType in queuedSystemTypes)
                {
                    world.GetOrCreate(systemType);
                }
            }
        }

        public void AddInstance<TInstance>(in TInstance instance)
            where TInstance : Instance
        {
            using (SynchronizeThread())
            {
                OnInstanceAdded(in instance);
            }
        }

        protected virtual void OnInstanceAdded<TInstance>(in TInstance instance)
            where TInstance : Instance
        {
            var worldCollection = new WorldCollection(Context, new World());
            worldCollection.Ctx.Bind<IManagedWorldTime, ManagedWorldTime>();
            worldCollection.Ctx.Bind<IScheduler, Scheduler>(GetScheduler());

            foreach (var type in systemTypes)
            {
                worldCollection.GetOrCreate(type);
            }

            MappedWorldCollection[instance] = worldCollection;
        }

        internal class TimeSystem : IWorldSystem
        {
            private Entity          timeEntity;
            private WorldCollection worldCollection;

            [DependencyStrategy]
            public IManagedWorldTime WorldTime { get; set; }

            public WorldCollection WorldCollection
            {
                get => worldCollection;
                set
                {
                    worldCollection = value;

                    timeEntity = worldCollection.Mgr.CreateEntity();
                    worldCollection.Mgr.SetMaxCapacity<SingletonComponent<WorldTime>>(1);

                    timeEntity.Set(new SingletonComponent<WorldTime>());
                    timeEntity.Set(new WorldTime());
                    timeEntity.Set(WorldTime);
                }
            }

            public void Update(WorldTime time)
            {
                if (!timeEntity.IsAlive)
                {
                    return;
                }

                timeEntity.Set(time);
                if (WorldTime is ManagedWorldTime managed)
                {
                    managed.Update(timeEntity);
                }
            }
        }
    }
}
