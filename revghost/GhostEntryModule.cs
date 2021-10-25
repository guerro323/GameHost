using System;
using revghost.Injection;
using revghost.Injection.Dependencies;
using revghost.IO.Storage;
using revghost.Module;
using revghost.Module.Systems;
using revghost.Threading.Systems;

namespace revghost;

public class GhostEntryModule : HostModule
{
    private HostRunnerScope _hostScope;

    public GhostEntryModule(HostRunnerScope scope) : base(scope)
    {
        _hostScope = scope;

        _hostScope.Context.Register(new ModuleManager(_hostScope));
        _hostScope.Context.Register(new ManageModuleRequestSystem(_hostScope));
        _hostScope.Context.Register(new GatherModuleSystem(_hostScope));
        _hostScope.Context.Register(new ReloadModuleOnFileChangeSystem(_hostScope));
        _hostScope.Context.Register(new AddListenerToCollectionSystem(_hostScope));
        _hostScope.Context.Register(new RemoveDisposedListenerCollectionsSystem(_hostScope));
        _hostScope.Context.Register(new UpdateLocalThreadedCollectionSystem(_hostScope));

        // This module should only be finalized once these systems are fullfilled dependency wise
        Dependencies.Add(new Dependency(typeof(ModuleManager)));
        Dependencies.Add(new Dependency(typeof(ManageModuleRequestSystem)));
    }

    protected override void OnInit()
    {
    }

    protected internal override IStorage CreateDataStorage(Scope scope)
    {
        if (!scope.Context.TryGet(out IStorage executingStorage))
            throw new NullReferenceException(nameof(IStorage));

        return executingStorage.GetSubStorage("Config");
    }
}

public static class GhostEntryModuleExtensions
{
    public static HostModule AddEntryModule(this GhostRunner runner)
    {
        var entity = runner.Scope.World.CreateEntity();
        entity.Set(new RegisteredModule());
        {
            entity.Set(ModuleState.Loaded);
            entity.Set(new HostModuleDescription("GameHost", "Entry"));
        }

        entity.Set(typeof(GhostEntryModule).Assembly);

        var module = new GhostEntryModule(runner.Scope);
        entity.Set<HostModule>(module);

        return module;
    }
}