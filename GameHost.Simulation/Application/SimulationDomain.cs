using System;
using System.Diagnostics;
using DefaultEcs;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.Utility.EntitySystem;
using GameHost.V3;
using GameHost.V3.Domains;
using GameHost.V3.Domains.Time;
using GameHost.V3.Injection;
using GameHost.V3.Loop;
using GameHost.V3.Loop.EventSubscriber;
using GameHost.V3.Threading;
using GameHost.V3.Threading.V2;
using GameHost.V3.Threading.V2.Apps;

namespace GameHost.Simulation.Application
{
    public class SimulationDomain : CommonDomainThreadListener
    {
        public readonly SimulationScope Scope;

        public readonly GameWorld GameWorld;
        public readonly World World;

        public readonly IBatchRunner BatchRunner;
        private readonly ThreadBatchRunner _batchRunner;

        public readonly IManagedWorldTime WorldTime;
        private readonly ManagedWorldTime _worldTime;

        public readonly IDomainUpdateLoopSubscriber UpdateLoop;
        private readonly DefaultDomainUpdateLoopSubscriber _updateLoop;

        private readonly Stopwatch _sleepTime = new();
        private readonly DomainWorker _worker;
 
        public TimeSpan? TargetFrequency
        {
            get => _targetFrequency;
            set => Scheduler.Add(args => args.t._targetFrequency = args.v, (t: this, v: value));
        }

        private TimeSpan? _targetFrequency;
        private FixedTimeStep _fts;

        private int _currentFrame;

        public SimulationDomain(Scope scope, Entity domainEntity) : base(scope, domainEntity)
        {
            Scope = new SimulationScope(DomainScope);
            {
                World = Scope.World;
                GameWorld = Scope.GameWorld;

                Scope.Context.Register(BatchRunner = _batchRunner = new ThreadBatchRunner(0.5f));
                Scope.Context.Register(WorldTime = _worldTime = new ManagedWorldTime());
                Scope.Context.Register(UpdateLoop = _updateLoop = new DefaultDomainUpdateLoopSubscriber(World));
            }

            _targetFrequency = TimeSpan.FromMilliseconds(10);

            if (!scope.Context.TryGet(out _worker))
                _worker = new DomainWorker("Simulation Domain");
        }

        protected override void DomainUpdate()
        {
            // future proof for a rollback system
            _worldTime.Total = _currentFrame * _worldTime.Delta;
            {
                _updateLoop.Invoke(_worldTime.Total, _worldTime.Delta);
            }
        }

        protected override ListenerUpdate OnUpdate()
        {
            if (IsDisposed || _disposalStartTask.Task.IsCompleted)
                return default;

            var delta = _worker.Delta + _sleepTime.Elapsed;

            var updateCount = 1;
            if (_targetFrequency is { } targetFrequency)
            {
                updateCount = Math.Min(_fts.GetUpdateCount(delta.TotalSeconds), 3);
            }

            // If we don't have a target frequency, use the delta
            using (_worker.StartMonitoring(_targetFrequency ?? delta))
            {
                _worldTime.Delta = _targetFrequency ?? delta;

                using (CurrentUpdater.SynchronizeThread())
                {
                    Scheduler.Run();
                    TryExecuteScheduler();

                    try
                    {
                        _batchRunner.StartPerformanceCriticalSection();
                        for (var tickAge = updateCount - 1; tickAge >= 0; --tickAge)
                        {
                            _currentFrame++;
                            DomainUpdate();
                        }
                    }
                    finally
                    {
                        _batchRunner.StopPerformanceCriticalSection();
                    }
                }
            }

            var timeToSleep =
                TimeSpan.FromTicks(
                    Math.Max(
                        (_targetFrequency ?? TimeSpan.FromMilliseconds(1)).Ticks - _worker.Delta.Ticks,
                        0
                    )
                );

            _sleepTime.Restart();
            return new ListenerUpdate
            {
                TimeToSleep = timeToSleep
            };
        }

        public override void Dispose()
        {
            base.Dispose();

            Scope.Dispose();
            {
                _batchRunner.Dispose();
            }
        }
    }
}