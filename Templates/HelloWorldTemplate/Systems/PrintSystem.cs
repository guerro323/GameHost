using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using HelloWorldTemplate.Components;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace HelloWorldTemplate.Systems
{
	/// <summary>
	/// This system will print any entities that has an <see cref="PrintTextComponent"/>.
	/// Once done, it will delete all entities with that component.
	/// </summary>
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class PrintSystem : AppSystem
	{
		private ILogger logger;

		private readonly EntitySet entitySet;

		public PrintSystem(WorldCollection collection) : base(collection)
		{
			// Get the logger, for logging to the console (we don't use Console.WriteLine)
			DependencyResolver.Add(() => ref logger);

			// Get a set that will return entities with PrintTextComponent
			entitySet = World.Mgr.GetEntities()
			                 .With<PrintTextComponent>()
			                 .AsSet();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (var entity in entitySet.GetEntities())
				logger.ZLogInformation(entity.Get<PrintTextComponent>().Value);

			// Dispose (destroy/remove) all entities from this set
			entitySet.DisposeAllEntities();
		}
	}
}