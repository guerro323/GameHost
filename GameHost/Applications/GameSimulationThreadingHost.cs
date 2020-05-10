using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using DefaultEcs;
using DryIoc;
using GameHost.Core.Ecs;
using GameHost.Core.Game;
using GameHost.Core.Threading;
using GameHost.Entities;

namespace GameHost.Applications
{
    public class GameSimulationThreadingHost : ThreadingHost<GameSimulationThreadingHost>
    {
        private Dictionary<Instance, WorldCollection> worldCollectionPerInstance;

        public int Frame { get; private set; }

        public TimeSpan Frequency
        {
            get
            {
                using (SynchronizeThread())
                    return frequency;
            }
            set
            {
                ThreadingHost.Synchronize<GameSimulationThreadingHost, TimeSpan, TimeSpan>(SetFrequency, value, null, value, cc: CancellationToken);
            }
        }

        private void SetFrequency(TimeSpan value) => frequency = value;

        private TimeSpan frequency;
        private Entity   timeEntity;

        private List<Type> standardSystemTypes;
        private List<Type> queuedSystems;

        public GameSimulationThreadingHost(float frequency)
        {
            worldCollectionPerInstance = new Dictionary<Instance, WorldCollection>(8);

            standardSystemTypes = new List<Type>(128);
            queuedSystems = new List<Type>(128);
            AppSystemResolver.ResolveFor<GameSimulationThreadingHost>(standardSystemTypes);

            this.frequency = TimeSpan.FromSeconds(frequency);
        }

        protected override void OnThreadStart()
        {
            var updateSw = new Stopwatch();
            var totalSw  = new Stopwatch();

            var managedWorldTime = new ManagedWorldTime();

            totalSw.Start();

            var elapsedTime = TimeSpan.Zero;
            var fts         = new FixedTimeStep {TargetFrameTimeMs = Frequency.Milliseconds};
            do
            {
                GetScheduler().Run();
                
                var spanDt = updateSw.Elapsed;
                updateSw.Restart();

                // Make sure no one can mess with thread safety here
                var updateCount = fts.GetUpdateCount(spanDt.TotalSeconds);
                updateCount = Math.Min(updateCount, 4);

                if (queuedSystems.Count > 0)
                {
                    using (SynchronizeThread())
                    {
                        foreach (var world in worldCollectionPerInstance.Values)
                        {
                            foreach (var systemType in queuedSystems)
                                world.GetOrCreate(systemType);
                        }
                    }

                    standardSystemTypes.AddRange(queuedSystems);
                    queuedSystems.Clear();
                }

                for (var i = 0; i < updateCount; i++)
                {
                    Frame++;
                    using (SynchronizeThread())
                    {
                        foreach (var world in worldCollectionPerInstance.Values)
                        {
                            if (!timeEntity.IsAlive)
                            {
                                timeEntity = world.Mgr.CreateEntity();
                                timeEntity.Set(new WorldTime());

                                world.Ctx.Container.UseInstance<IManagedWorldTime>(managedWorldTime);
                            }

                            timeEntity.Set(new WorldTime {Delta = frequency, Total = elapsedTime});
                            managedWorldTime.Update(timeEntity);

                            world.DoInitializePass();
                            world.DoUpdatePass();
                        }
                    }
                }

                const string simulation = "simulation";
                if (updateCount > 0)
                    GamePerformance.SetElapsedDelta(simulation, spanDt);

                elapsedTime = totalSw.Elapsed;

                var wait = frequency - spanDt;
                if (wait > TimeSpan.Zero)
                    CancellationToken.WaitHandle.WaitOne(wait);
            } while (!CancellationToken.IsCancellationRequested);
        }

        public void InjectInstance(Instance instance)
        {
            const IfUnresolved ifu = IfUnresolved.ReturnDefaultIfNotRegistered;
            
            using (SynchronizeThread())
            {
                var worldCollection = new WorldCollection(instance.Context, instance.Context.Container.Resolve<World>(ifu) ?? new World());
                worldCollection.Ctx.Bind<IManagedWorldTime, ManagedWorldTime>();
                worldCollection.Ctx.Bind<IScheduler, Scheduler>(GetScheduler());
                
                foreach (var type in standardSystemTypes)
                    worldCollection.GetOrCreate(type);

                worldCollectionPerInstance[instance] = worldCollection;
            }
        }

        public void InjectAssembly(Assembly assembly)
        {
            using (SynchronizeThread())
            {
                AppSystemResolver.ResolveFor<GameSimulationThreadingHost>(assembly, queuedSystems);
            }
        }
    }

    public class GameSimulationThreadingClient : ThreadingClient<GameSimulationThreadingHost>
    {
        public void InjectAssembly(Assembly assembly)
        {
            Listener.InjectAssembly(assembly);
        }

        public void InjectInstance(Instance instance)
        {
            Listener.InjectInstance(instance);
        }
    }
}
