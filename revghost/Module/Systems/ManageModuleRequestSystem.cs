using System;
using DefaultEcs;
using revghost.Domains.Time;
using revghost.Ecs;
using revghost.Injection.Dependencies;
using revghost.Loop.EventSubscriber;
using revghost.Shared.Threading.Schedulers;
using revghost.Threading;
using revghost.Utility;

namespace revghost.Module.Systems;

public class ManageModuleRequestSystem : AppSystem
{
    private IScheduler _scheduler;
    private World _world;

    private ModuleManager _moduleManager;

    private IDomainUpdateLoopSubscriber _domainUpdateLoop;

    private readonly HostLogger _logger = new(nameof(ManageModuleRequestSystem));

    public ManageModuleRequestSystem(Scope scope) : base(scope)
    {
        Dependencies.Add(() => ref _scheduler);
        Dependencies.Add(() => ref _world);
        Dependencies.Add(() => ref _moduleManager);
        Dependencies.Add(() => ref _domainUpdateLoop);
    }

    private EntitySet _loadSet;
    private EntitySet _reloadSet;

    protected override void OnInit()
    {
        Disposables.AddRange(new IDisposable[]
        {
            _loadSet = _world.GetEntities()
                .With<RequestLoadModule>()
                .AsSet(),

            _reloadSet = _world.GetEntities()
                .With<RequestReloadModule>()
                .AsSet(),

            _domainUpdateLoop.Subscribe(OnUpdate)
        });
    }

    private void OnUpdate(WorldTime worldTime)
    {
        if (_loadSet.Count == 0 && _reloadSet.Count == 0)
            return;
            
        foreach (var entity in _loadSet.GetEntities())
        {
            var request = entity.Get<RequestLoadModule>();
            if (!request.Module.IsAlive)
                throw new InvalidOperationException($"Module Entity was destroyed (Given Name: {request.Name})");

            if (request.Module.Get<ModuleState>() != ModuleState.None)
                continue; // should we report that?

            _scheduler.Add(args => args.mgr.LoadModule(args.mod), (mgr: _moduleManager, mod: request.Module), true);
        }

        foreach (var entity in _reloadSet.GetEntities())
        {
            var request = entity.Get<RequestReloadModule>();
            if (!request.Module.IsAlive)
                throw new InvalidOperationException($"Module Entity was destroyed (Given Name: {request.Name})");
                
            _logger.Info($"Unloading Module (request={request.Name}, entity={request.Module})", "module-unloading");
            _scheduler.Add(args =>
            {
                if (args.mod.Get<ModuleState>() != ModuleState.Loaded)
                {
                    _logger.Warn(
                        $"Module (request={request.Name}, entity={request.Module}) was not in an unloaded state (current={args.mod.Get<ModuleState>()})",
                        "module-unloading"
                    );
                    return;
                }
                    
                args.mgr.UnloadModule(args.mod);
                _scheduler.Add(args =>
                {
                    if (args.mod.Get<ModuleState>() == ModuleState.Unloading)
                        return false;

                    args.mgr.LoadModule(args.mod);
                    return true;
                }, args, SchedulingParametersWithArgs.AsOnceWithArgs);
            }, (mgr: _moduleManager, mod: request.Module), true);
        }

        _loadSet.DisposeAllEntities();
        _reloadSet.DisposeAllEntities();
    }
}