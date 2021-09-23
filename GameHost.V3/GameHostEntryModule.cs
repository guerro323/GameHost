using System;
using GameHost.V3.Injection;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.IO.Storage;
using GameHost.V3.Module;
using GameHost.V3.Module.Systems;
using GameHost.V3.Threading.Systems;

namespace GameHost.V3
{
    public class GameHostEntryModule : HostModule
    {
        private HostRunnerScope _hostScope;

        public GameHostEntryModule(HostRunnerScope scope) : base(scope)
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

    public static class GameHostEntryModuleExtensions
    {
        public static HostModule AddEntryModule(this HostRunner runner)
        {
            var entity = runner.Scope.World.CreateEntity();
            entity.Set(new RegisteredModule());
            {
                entity.Set(ModuleState.Loaded);
                entity.Set(new HostModuleDescription("GameHost", "Entry"));
            }

            entity.Set(typeof(GameHostEntryModule).Assembly);

            var module = new GameHostEntryModule(runner.Scope);
            entity.Set<HostModule>(module);

            return module;
        }
    }
}