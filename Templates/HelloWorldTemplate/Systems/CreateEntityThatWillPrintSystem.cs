using System.Collections.Generic;
using GameHost.Applications;
using GameHost.Core.Ecs;
using HelloWorldTemplate.Components;
using HelloWorldTemplate.Data;

namespace HelloWorldTemplate.Systems
{
	/// <summary>
	/// This system will create an entity with PrintTextComponent.
	/// It will then get treated in <see cref="PrintSystem"/>
	/// </summary>
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class CreateEntityThatWillPrintSystem : AppSystem
	{
		private PrintConfiguration printConfiguration;

		public CreateEntityThatWillPrintSystem(WorldCollection collection) : base(collection)
		{
			// Get the configuration as a dependency
			DependencyResolver.Add(() => ref printConfiguration);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			// Loop into the text to print...
			foreach (var str in printConfiguration.TextsToPrint)
			{
				// Create entity
				var entity = World.Mgr.CreateEntity();
				// Set the component to that entity
				entity.Set(new PrintTextComponent(str));
			}
		}
	}
}