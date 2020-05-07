using System;
using System.Reflection;
using System.Runtime.Loader;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Core.Modding.Systems;
using GameHost.Injection;

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
        private EntitySet loadSet, unloadSet;

        private ModuleManager moduleMgr;

        [DependencyStrategy]
        public IStorage Storage { get; set; }

        [DependencyStrategy]
        public AssemblyLoadContext AssemblyLoadContext { get; set; }

        protected override void OnInit()
        {
            base.OnInit();
            loadSet   = World.Mgr.GetEntities().With<RequestLoadModule>().AsSet();
            unloadSet = World.Mgr.GetEntities().With<RequestUnloadModule>().AsSet();

            moduleMgr = World.GetOrCreate<ModuleManager>();

            Storage.GetOrCreateDirectoryAsync("Modules").ContinueWith(task => this.Storage = new ModuleStorage(task.Result));
        }

        // todo: base.CanUpdate() should also include dependencies
        public override bool CanUpdate() => base.CanUpdate() && (Storage as ModuleStorage) != null && AssemblyLoadContext != null;

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
