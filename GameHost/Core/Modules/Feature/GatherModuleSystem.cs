using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using GameHost.Core.Threading;
using NetFabric.Hyperlinq;

namespace GameHost.Core.Modules.Feature
{
    [RestrictToApplication(typeof(ExecutiveEntryApplication))]
    public class GatherAvailableModuleSystem : AppSystemWithFeature<ModuleLoaderFeature>
    {
        private IStorage   storage;
        private IScheduler scheduler;

        public GatherAvailableModuleSystem(WorldCollection worldCollection) : base(worldCollection)
        {
            DependencyResolver.Add(() => ref storage);
            DependencyResolver.Add(() => ref scheduler);
        }

        protected override async void OnFeatureAdded(ModuleLoaderFeature obj)
        {
            storage = new ModuleStorage(await storage.GetOrCreateDirectoryAsync("Modules"));
        }

        private EntitySet refreshSet, moduleSet;

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
        }

        private bool isTaskRunning;

        public override bool CanUpdate() =>
            base.CanUpdate()
            && !isTaskRunning           // can not update if there is already a task running 
            && storage is ModuleStorage // can not update if we have an invalid or non-existant storage
            && refreshSet.Count > 0;    // can not update if there are no request...

        protected override async void OnUpdate()
        {
            base.OnUpdate();

            isTaskRunning = true;
            var files = (await storage.GetFilesAsync("*.dll")).ToList();
            // Destroy unloaded modules...
            scheduler.Schedule(DestroyUnloadedModules, SchedulingParameters.AsOnce);

            foreach (var file in files)
            {
                var assemblyName = file.Name.Replace(".dll", "");
                var rm           = FindOrCreateEntity(assemblyName);
                // We have found an already existing module, does not do further operation on it...
                if (rm.Has<RegisteredModule>() && rm.Get<RegisteredModule>().State != ModuleState.None)
                    continue;

                Console.WriteLine($"Discovered {assemblyName}");
                scheduler.Schedule(moduleEntity =>
                {
                    moduleEntity.Set(new RegisteredModule {Description = {NameId = assemblyName, DisplayName = assemblyName, Author = "not-loaded"}});
                    moduleEntity.Set(file);
                }, rm, default);
            }

            // Finally destroy refresh requests...
            scheduler.Schedule(refreshSet.DisposeAllEntities, SchedulingParameters.AsOnce);

            isTaskRunning = false;
        }

        /// <summary>
        /// Find an existing entity with an assembly name or create a new one.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <returns>An existing or new entity.</returns>
        private Entity FindOrCreateEntity(string assemblyName)
        {
            return moduleSet.GetEntities()
                            .Where(e => e.Get<RegisteredModule>().Description.NameId == assemblyName)
                            .First()
                            .Match(e => e, World.Mgr.CreateEntity);
        }

        /// <summary>
        /// Destroy modules that are 100% not loaded. (no trace left in GC)
        /// </summary>
        private void DestroyUnloadedModules()
        {
            foreach (var ent in moduleSet.GetEntities())
            {
                if (ent.Get<RegisteredModule>().State == ModuleState.None)
                    ent.Dispose();
            }
        }
    }
}