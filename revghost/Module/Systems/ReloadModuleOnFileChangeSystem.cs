using DefaultEcs;
using revghost.Domains.Time;
using revghost.Ecs;
using revghost.Injection.Dependencies;
using revghost.IO.Storage;
using revghost.Loop.EventSubscriber;
using revghost.Utility;

namespace revghost.Module.Systems;

public class ReloadModuleOnFileChangeSystem : AppSystem
{
    private World _world;
    private IDomainUpdateLoopSubscriber _updateLoop;

    private readonly HostLogger _logger = new(nameof(ReloadModuleOnFileChangeSystem));
        
    public ReloadModuleOnFileChangeSystem(Scope scope) : base(scope)
    {
        Dependencies.AddRef(() => ref _world);
        Dependencies.AddRef(() => ref _updateLoop);
    }

    private EntitySet _notifySet;

    protected override void OnInit()
    {
        Disposables.Add(_notifySet = _world.GetEntities()
            .With<RegisteredModule>()
            .With<ModuleState>()
            .WhenChanged<IFile>()
            .AsSet());

        Disposables.Add(_updateLoop.Subscribe(OnUpdate));
    }

    private void OnUpdate(WorldTime time)
    {
        foreach (var module in _notifySet.GetEntities())
        {
            _logger.Info(
                $"Will reload {module.Get<HostModuleDescription>().ToPath()} (entity={module})",
                "module-reload-request"
            );
                
            _world.CreateEntity()
                .Set(new RequestReloadModule($"{module.Get<HostModuleDescription>().ToPath()}", module));
        }

        _notifySet.Complete();
    }
}