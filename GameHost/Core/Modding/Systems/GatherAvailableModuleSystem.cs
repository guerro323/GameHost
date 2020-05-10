﻿using System;
using System.Collections.Generic;
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
using GameHost.Core.Threading;
using GameHost.Entities;
using GameHost.Injection;
using GameHost.IO;
using NetFabric.Hyperlinq;

namespace GameHost.Core.Modding.Systems
{
    [RestrictToApplication(typeof(MainThreadHost))]
    public class GatherAvailableModuleSystem : AppSystem
    {
        private IStorage   storage;
        private IScheduler scheduler;

        public GatherAvailableModuleSystem(WorldCollection worldCollection) : base(worldCollection)
        {
            DependencyResolver.Add(() => ref storage);
            DependencyResolver.Add(() => ref scheduler);
        }

        protected override async void OnDependenciesResolved(IEnumerable<object> dependencies)
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
            scheduler.AddOnce(DestroyUnloadedModules);

            foreach (var file in files)
            {
                var assemblyName = file.Name.Replace(".dll", "");
                var rm           = FindOrCreateEntity(assemblyName);
                // We have found an already existing module, does not do further operation on it...
                if (rm.Has<RegisteredModule>() && rm.Get<RegisteredModule>().State != ModuleState.None)
                    continue;

                scheduler.Add(() =>
                {
                    rm.Set(new RegisteredModule {Info = {NameId = assemblyName, DisplayName = assemblyName, Author = "not-loaded"}});
                    rm.Set(file);
                });
            }

            // Finally destroy refresh requests...
            scheduler.AddOnce(refreshSet.DisposeAllEntities);

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
                            .Where(e => e.Get<RegisteredModule>().Info.NameId == assemblyName)
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
