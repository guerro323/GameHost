using System.Linq;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.Threading;
using Microsoft.Extensions.Logging;
using NetFabric.Hyperlinq;
using ZLogger;

namespace GameHost.Core.Modules.Feature
{
    [RestrictToApplication(typeof(ExecutiveEntryApplication))]
    public class GatherAvailableModuleSystem : AppSystemWithFeature<ModuleLoaderFeature>
    {
        private ILogger       logger;
        private ModuleStorage moduleStorage;
        private IScheduler    scheduler;

        public GatherAvailableModuleSystem(WorldCollection worldCollection) : base(worldCollection)
        {
            DependencyResolver.Add(() => ref logger);
            DependencyResolver.Add(() => ref moduleStorage);
            DependencyResolver.Add(() => ref scheduler);
        }

        protected override async void OnFeatureAdded(ModuleLoaderFeature obj)
        {
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
            && Features.Any()        // can not update if there are no features...
            && !isTaskRunning        // can not update if there is already a task running 
            && refreshSet.Count > 0; // can not update if there are no request...

        protected override async void OnUpdate()
        {
            base.OnUpdate();

            isTaskRunning = true;
            var files = (await moduleStorage.GetFilesAsync("*.dll")).ToList();
            // Destroy unloaded modules...
            scheduler.Schedule(DestroyUnloadedModules, SchedulingParameters.AsOnce);

            foreach (var file in files)
            {
                var assemblyName = file.Name.Replace(".dll", "");
                var rm           = FindOrCreateEntity(assemblyName, out var wasCreated);
                // We have found an already existing module, does not do further operation on it...
                if (rm.Has<RegisteredModule>() && rm.Get<RegisteredModule>().State != ModuleState.None)
                    continue;

                scheduler.Schedule(moduleEntity =>
                {
                    logger.ZLogInformation($"Discovered Module {assemblyName}.dll (fullPath={file.FullName})");
                    
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
        private Entity FindOrCreateEntity(string assemblyName, out bool wasCreated)
        {
            wasCreated = false;
            foreach (var entity in moduleSet.GetEntities())
            {
                if (entity.Get<RegisteredModule>().Description.NameId == assemblyName)
                    return entity;
            }

            wasCreated = true;
            return World.Mgr.CreateEntity();
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