using System;
using System.Collections.Generic;
using System.Diagnostics;
using DefaultEcs;
using DryIoc;
using GameHost.Core.Ecs;
using GameHost.Core.Game;
using GameHost.Core.Threading;
using GameHost.Entities;
using GameHost.Injection;

namespace GameHost.Applications
{
    public abstract class GameThreadedHostApplicationBase<T> : ThreadingHost<T>
    {
        protected bool    QuitApplication { get; set; }
        protected Context Context         { get; }

        protected List<Type> systemTypes       = new List<Type>();
        protected List<Type> queuedSystemTypes = new List<Type>();

        protected virtual bool autoResolveSystem => true;

        protected Dictionary<Instance, WorldCollection> MappedWorldCollection = new Dictionary<Instance, WorldCollection>(1);

        private TimeSpan frequency;

        public TimeSpan Frequency
        {
            get
            {
                using (SynchronizeThread())
                    return frequency;
            }
            set
            {
                ThreadingHost.Synchronize<GameSimulationThreadingHost, TimeSpan, TimeSpan>(f => frequency = f, value, null, value, cc: CancellationToken);
            }
        }

        protected GameThreadedHostApplicationBase(Context context, TimeSpan? frequency = null)
        {
            this.frequency = frequency ?? TimeSpan.FromSeconds(1f / 1000f);
            this.Context   = context;
            
            if (autoResolveSystem)
                AppSystemResolver.ResolveFor<T>(systemTypes);
        }

        protected Stopwatch UpdateStopwatch, TotalStopwatch;

        protected override void OnThreadStart()
        {
            UpdateStopwatch = new Stopwatch();
            TotalStopwatch  = new Stopwatch();

            OnInit();

            var elapsedTime = TimeSpan.Zero;
            var fts         = new FixedTimeStep {TargetFrameTimeMs = Frequency.Milliseconds};
            while (!CancellationToken.IsCancellationRequested && !QuitApplication)
            {
                var spanDt = UpdateStopwatch.Elapsed;
                UpdateStopwatch.Restart();

                GetScheduler().Run();

                if (queuedSystemTypes.Count > 0)
                {
                    using (SynchronizeThread())
                    {
                        OnQueuedSystemsAdded();
                    }

                    systemTypes.AddRange(queuedSystemTypes);
                    queuedSystemTypes.Clear();
                }

                // Make sure no one can mess with thread safety here
                var updateCount = fts.GetUpdateCount(spanDt.TotalSeconds);
                OnUpdate(ref updateCount, elapsedTime.TotalSeconds);

                elapsedTime = TotalStopwatch.Elapsed;

                var wait = frequency - spanDt;
                if (wait > TimeSpan.Zero)
                    CancellationToken.WaitHandle.WaitOne(wait);
            }

            OnQuit();
        }

        protected abstract void OnInit();

        protected virtual void OnUpdate(ref int fixedUpdateCount, double elapsedTime)
        {
            for (var i = 0; i != fixedUpdateCount; i++)
                OnFixedUpdate(i, (float)frequency.TotalSeconds, elapsedTime);
        }

        protected virtual void OnFixedUpdate(int step, float delta, double elapsedTime)
        {
            foreach (var world in MappedWorldCollection.Values)
            {
                world.DoInitializePass();
                var timeSystem = world.GetOrCreate<TimeSystem>();
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
                    world.GetOrCreate(systemType);
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

            foreach (var type in systemTypes)
                worldCollection.GetOrCreate(type);

            MappedWorldCollection[instance] = worldCollection;
        }

        internal class TimeSystem : IWorldSystem
        {
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

            [DependencyStrategy]
            public IManagedWorldTime WorldTime { get; set; }

            private Entity timeEntity;
            private WorldCollection worldCollection;

            public void Update(WorldTime time)
            {
                if (!timeEntity.IsAlive)
                    return;
                    
                timeEntity.Set(time);
                if (WorldTime is ManagedWorldTime managed)
                {
                    managed.Update(timeEntity);
                }
            }
        }
    }
}
