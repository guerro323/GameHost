using System;
using System.Diagnostics;
using DefaultEcs;
using revghost.Domains.Time;
using revghost.Loop;
using revghost.Loop.EventSubscriber;
using revghost.Shared.Threading.Schedulers;
using revghost.Threading;

namespace revghost.Domains;

// TODO: are executive domain (aka ExecutiveEntryApplication) still needed?
public class ExecutiveDomain : IDomain, IDisposable
{
    public readonly IDomainUpdateLoopSubscriber UpdateLoop;
    public readonly IManagedWorldTime WorldTime;

    private readonly DefaultDomainUpdateLoopSubscriber _updateLoop;
    private readonly ManagedWorldTime _worldTime;

    private readonly GhostRunner _runner;

    private TimeSpan _previousElapsed;
    private Stopwatch _elapsedSw;

    public ExecutiveDomain(GhostRunner runner)
    {
        _runner = runner;

        DomainEntity = runner.Scope.World.CreateEntity();

        _elapsedSw = new Stopwatch();

        runner.Scope.Context.Register(WorldTime = _worldTime = new ManagedWorldTime());
        runner.Scope.Context.Register(UpdateLoop =
            _updateLoop = new DefaultDomainUpdateLoopSubscriber(runner.Scope.World));
    }

    public void Dispose()
    {
        _updateLoop.Dispose();
        DomainEntity.Dispose();
    }

    public Entity DomainEntity { get; }

    public bool Loop()
    {
        if (!_elapsedSw.IsRunning)
            _elapsedSw.Start();

        {
            var previous = _worldTime.Total;
                
            _worldTime.Total = _elapsedSw.Elapsed;
            _worldTime.Delta = _worldTime.Total - previous;
                
            _updateLoop.Invoke(_worldTime.Total, _worldTime.Delta);
        }

        if (_runner.Scope.Scheduler is IRunnableScheduler runnableScheduler)
            runnableScheduler.Run();

        return true;
    }
}