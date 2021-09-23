using System;
using DefaultEcs;
using GameHost.V3.Domains.Time;
using GameHost.V3.Ecs;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.IO.Storage;
using GameHost.V3.Loop.EventSubscriber;

namespace GameHost.V3.Module.Systems
{
    public class ReloadModuleOnFileChangeSystem : AppSystem
    {
        private World _world;
        private IDomainUpdateLoopSubscriber _updateLoop;

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
                Console.WriteLine("reload!");
                _world.CreateEntity()
                    .Set(new RequestReloadModule($"{module.Get<HostModuleDescription>().ToPath()}", module));
            }

            _notifySet.Complete();
        }
    }
}