using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.Threading;
using Microsoft.Extensions.Logging;
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
            // no? scheduler.Schedule(DestroyUnloadedModules, SchedulingParameters.AsOnce);

            foreach (var file in files)
            {
                var assemblyName = file.Name.Replace(".dll", "");
                var (rm, _) = await FindOrCreateEntity(assemblyName);
                // We have found an already existing module, does not do further operation on it...
                if (rm.Has<RegisteredModule>() && rm.Get<RegisteredModule>().State != ModuleState.None)
                    continue;

                var jsonConfigurationFiles = await moduleStorage.GetFilesAsync(assemblyName + ".json");

                var jsonConfiguration = new ModuleConfigurationFile();
                foreach (var jsonFile in jsonConfigurationFiles)
                {
                    jsonConfiguration = JsonSerializer.Deserialize<ModuleConfigurationFile>(await jsonFile.GetContentAsync());
                    break;
                }

                scheduler.Schedule(moduleEntity =>
                {
                    logger.ZLogInformation($"Discovered Module\n\tAssembly={assemblyName}.dll\n\tPath={file.FullName}\n\tc.AutoLoad={jsonConfiguration.AutoLoad}");

                    moduleEntity.Set(new RegisteredModule { Description = { NameId = assemblyName, DisplayName = assemblyName, Author = "not-loaded" } });
                    moduleEntity.Set(file);

                    if (jsonConfiguration.AutoLoad)
                    {
                        // ReSharper disable VariableHidesOuterVariable
                        scheduler.Schedule(moduleEntity =>
                        {
                            World.Mgr.CreateEntity()
                                 .Set(new RequestLoadModule($"AutoLoad({moduleEntity.Get<RegisteredModule>().Description.NameId})", moduleEntity));
                        }, moduleEntity, default);
                        // ReSharper restore VariableHidesOuterVariable
                    }
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
        private async Task<(Entity module, bool wasCreated)> FindOrCreateEntity(string assemblyName)
        {
            var entityTcs     = new TaskCompletionSource<Entity>();
            var wasCreatedTcs = new TaskCompletionSource<bool>();
            scheduler.Schedule(() =>
            {
                foreach (var entity in moduleSet.GetEntities())
                {
                    if (entity.Get<RegisteredModule>().Description.NameId == assemblyName)
                    {
                        wasCreatedTcs.SetResult(false);
                        entityTcs.SetResult(entity);
                        return;
                    }
                }

                wasCreatedTcs.SetResult(true);
                entityTcs.SetResult(World.Mgr.CreateEntity());
            }, default);

            return (await entityTcs.Task, await wasCreatedTcs.Task);
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