using System;
using DefaultEcs;
using GameHost.V3.Domains.Time;
using GameHost.V3.Ecs;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.Loop.EventSubscriber;
using GameHost.V3.Threading;
using GameHost.V3.Utility;

namespace GameHost.V3.Module.Systems
{
    public class ManageModuleRequestSystem : AppSystem
    {
        private IScheduler _scheduler;
        private World _world;

        private ModuleManager _moduleManager;

        private IDomainUpdateLoopSubscriber _domainUpdateLoop;

        public ManageModuleRequestSystem(Scope scope) : base(scope)
        {
            Dependencies.AddRef(() => ref _scheduler);
            Dependencies.AddRef(() => ref _world);
            Dependencies.AddRef(() => ref _moduleManager);
            Dependencies.AddRef(() => ref _domainUpdateLoop);
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

                Console.WriteLine("Start Unloading");
                _scheduler.Add(args =>
                {
                    if (args.mod.Get<ModuleState>() != ModuleState.Loaded)
                    {
                        Console.WriteLine("not in an unloaded state: " + args.mod.Get<ModuleState>());
                        return;
                    }
                    
                    Console.WriteLine("Unloading on Scheduler");
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
}