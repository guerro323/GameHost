using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Core.Modding.Systems;

namespace GameHost.Core.Modding.Components
{
    public struct RequestLoadModule
    {
        public Entity Module;
    }

    public struct RequestUnloadModule
    {
        public Entity Module;
    }

    [RestrictToApplication(typeof(MainThreadHost))]
    public class ManageModuleLoadSystem : AppSystem
    {
        private IStorage storage;
        private AssemblyLoadContext assemblyLoadContext;
        private ModuleManager moduleMgr;
        
        public ManageModuleLoadSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref storage);
            DependencyResolver.Add(() => ref assemblyLoadContext);
            DependencyResolver.Add(() => ref moduleMgr);
        }
        
        private EntitySet loadSet, unloadSet;

        protected override void OnInit()
        {
            base.OnInit();
            loadSet   = World.Mgr.GetEntities().With<RequestLoadModule>().AsSet();
            unloadSet = World.Mgr.GetEntities().With<RequestUnloadModule>().AsSet();
        }

        protected override async void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            storage = new ModuleStorage(await storage.GetOrCreateDirectoryAsync("Modules"));
        }

        public override bool CanUpdate() => base.CanUpdate() && storage is ModuleStorage;

        protected override void OnUpdate()
        {
            foreach (var entity in loadSet.GetEntities())
            {
                var request = entity.Get<RequestLoadModule>();
                if (request.Module.Get<RegisteredModule>().State != ModuleState.None)
                    continue; // should we report that?

                try
                {
                    moduleMgr.LoadModule(request.Module);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    entity.Dispose();
                }
            }
        }
    }
}
