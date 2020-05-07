using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Core.Modding.Components;
using GameHost.Injection;

namespace GameHost.Core.Modding.Systems
{
    [RestrictToApplication(typeof(MainThreadHost))]
    public class GatherAvailableModuleSystem : AppSystem
    {
        private IStorage  trStorage;
        private EntitySet refreshSet, moduleSet;

        private bool isTaskRunning;
        
        protected override void OnInit()
        {
            base.OnInit();

            refreshSet = World.Mgr.GetEntities()
                              .With<RefreshModuleList>()
                              .AsSet();

            moduleSet = World.Mgr.GetEntities()
                             .With<RegisteredModule>()
                             .AsSet();
            
            World.Mgr.CreateEntity().Set(new RefreshModuleList());

            var ctxStrategy = new ContextBindingStrategy(Context, true);
            var storage     = ctxStrategy.Resolve<IStorage>();

            storage.GetOrCreateDirectoryAsync("Modules").ContinueWith(task => trStorage = task.Result);
        }

        public override bool CanUpdate() =>
            base.CanUpdate()
            && !isTaskRunning // can not update if there is already a task running 
            && trStorage != null     // can not update if we have an invalid or non-existant storage
            && refreshSet.Count > 0; // can not update if there are no request...

        protected override async void OnUpdate()
        {
            base.OnUpdate();

            isTaskRunning = true;
            var files = (await trStorage.GetFilesAsync("*.dll")).ToList();
            // Destroy unloaded modules...
            DestroyUnloadedModules();

            Console.WriteLine("files found: " + files.Count);

            foreach (var file in files)
            {
                var rm = World.Mgr.CreateEntity();
                var assemblyName = file.Name.Replace(".dll", "");
                rm.Set(new RegisteredModule {Info = {NameId = assemblyName, DisplayName = assemblyName, Author = "not-loaded"}});
                rm.Set(file);
            }

            // Finally destroy refresh requests...
            DestroyEntities(refreshSet);

            isTaskRunning = false;
        }

        private void DestroyUnloadedModules()
        {
            foreach (var ent in moduleSet.GetEntities())
            {
                if (ent.Get<RegisteredModule>().State == ModuleState.None)
                    ent.Dispose();
            }
        }

        private void DestroyEntities(EntitySet set)
        {
            foreach (var ent in set.GetEntities()) ent.Dispose();
        }
    }
}
