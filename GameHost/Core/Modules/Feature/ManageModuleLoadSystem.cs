using System;
using System.Linq;
using System.Runtime.Loader;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;

namespace GameHost.Core.Modules.Feature
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	[UpdateAfter(typeof(ModuleManager))]
	public class ManageModuleLoadSystem : AppSystemWithFeature<ModuleLoaderFeature>
	{
		private ModuleStorage            moduleStorage;
		private AssemblyLoadContext assemblyLoadContext;
		private ModuleManager       moduleMgr;

		public ManageModuleLoadSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref moduleStorage);
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

		public override bool CanUpdate() => base.CanUpdate() && Features.Any();

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
			}
			
			loadSet.DisposeAllEntities();

			foreach (var entity in unloadSet.GetEntities())
			{
				var request = entity.Get<RequestUnloadModule>();
				if (request.Module.Get<RegisteredModule>().State != ModuleState.Loaded)
					continue; // should we report that?
				
				try
				{
					moduleMgr.UnloadModule(request.Module);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
			
			unloadSet.DisposeAllEntities();
		}
	}
}