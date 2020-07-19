using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;

namespace GameHost.Core.Modules.Feature
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	[UpdateAfter(typeof(ModuleManager))]
	public class ManageModuleLoadSystem : AppSystemWithFeature<ModuleLoaderFeature>
	{
		private IStorage            storage;
		private AssemblyLoadContext assemblyLoadContext;
		private ModuleManager       moduleMgr;

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

		protected override async void OnFeatureAdded(ModuleLoaderFeature obj)
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