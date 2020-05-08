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
        protected bool QuitApplication { get; set; }
        protected Context Context { get; }

        protected List<Type> systemTypes       = new List<Type>();
        protected List<Type> queuedSystemTypes = new List<Type>();

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
            this.Context = context;
        }

        protected Stopwatch UpdateStopwatch, TotalStopwatch;
        protected override void OnThreadStart()
        {
            UpdateStopwatch = new Stopwatch();
            TotalStopwatch = new Stopwatch();
            
            OnInit();

            var elapsedTime = TimeSpan.Zero;
            var fts = new FixedTimeStep {TargetFrameTimeMs = Frequency.Milliseconds};
            while (CancellationToken.IsCancellationRequested && !QuitApplication)
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
                OnUpdate(ref updateCount);
                
                var wait = frequency - spanDt;
                if (wait > TimeSpan.Zero)
                    CancellationToken.WaitHandle.WaitOne(wait);
            }

            OnQuit();
        }

        protected abstract void OnInit();

        protected virtual void OnUpdate(ref int fixedUpdateCount)
        {
            for (var i = 0; i != fixedUpdateCount; i++)
                OnFixedUpdate(i);
        }

        protected virtual void OnFixedUpdate(int step)
        {
            foreach (var world in MappedWorldCollection.Values)
            {
                world.DoInitializePass();
                var timeSystem = world.GetOrCreate<TimeSystem>();
                timeSystem.Update(new WorldTime {Delta = (float) frequency.TotalSeconds, Total = TotalStopwatch.Elapsed.TotalSeconds});
                world.DoUpdatePass();
            }
        }
        protected abstract void OnQuit();

        protected abstract void OnQueuedSystemsAdded();

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
        
        internal class TimeSystem : IInitSystem
        {
            public WorldCollection WorldCollection { get; set; }

            [DependencyStrategy]
            public IManagedWorldTime WorldTime { get; set; }

            private Entity timeEntity;

            public void OnInit()
            {
                timeEntity = WorldCollection.Mgr.CreateEntity();
                WorldCollection.Mgr.SetMaxCapacity<SingletonComponent<WorldTime>>(1);

                timeEntity.Set(new SingletonComponent<WorldTime>());
                timeEntity.Set(new WorldTime());
                timeEntity.Set(WorldTime);
            }

            public void Update(WorldTime time)
            {
                timeEntity.Set(time);
                if (WorldTime is ManagedWorldTime managed)
                {
                    managed.Update(timeEntity);
                }
            }
        }
    }
}
