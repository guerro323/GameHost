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

        void add<T>(T t)
            where T : IDisposable
        {
            _hostScope.Context.Register(t);
            Disposables.Add(t);
        }

        add(new ModuleManager(_hostScope));
        add(new ManageModuleRequestSystem(_hostScope));
        add(new GatherModuleSystem(_hostScope));
        add(new ReloadModuleOnFileChangeSystem(_hostScope));
        add(new AddListenerToCollectionSystem(_hostScope));
        add(new RemoveDisposedListenerCollectionsSystem(_hostScope));
        add(new UpdateLocalThreadedCollectionSystem(_hostScope));

        // This module should only be finalized once these systems are fullfilled dependency wise
        Dependencies.Add(new Dependency(typeof(ModuleManager)));
        Dependencies.Add(new Dependency(typeof(ManageModuleRequestSystem)));

        _hostScope.Context.Disposed += Dispose;
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