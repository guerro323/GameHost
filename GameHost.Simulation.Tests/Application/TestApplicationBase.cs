using System;
using System.Collections.Generic;
using System.Reflection;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Execution;
using GameHost.Core.Threading;
using GameHost.Simulation.Application;
using GameHost.Threading;
using GameHost.Worlds;
using NUnit.Framework;

namespace GameHost.Simulation.Tests.Application
{
	public class TestApplicationBase
	{
		public GlobalWorld Global;

		public virtual List<Type> RequiredExecutiveSystems { get; }
		public virtual List<Type> RequiredSimulationSystems { get; }

		public SimulationApplication RetrieveApplication()
		{
			return (SimulationApplication) Global.World.Get<IListener>()[0];
		}

		[SetUp]
		public void SetUp()
		{
			Global = new GlobalWorld();

			var systems = RequiredExecutiveSystems;
			if (systems == null)
			{
				systems = new List<Type>();

				// hack: make GameHost core systems being added to the resolver 
				var programType = typeof(GameHost.Program);
				systems.Add(programType);
				systems.Remove(programType);

				AppSystemResolver.ResolveFor<ExecutiveEntryApplication>(systems,
					t => t.GetCustomAttribute<RestrictToApplicationAttribute>()?.IsValid<ExecutiveEntryApplication>() == true);
			}
			
			foreach (var type in systems)
				Global.Collection.GetOrCreate(type);

			var listenerCollection = Global.World.CreateEntity();
			listenerCollection.Set<ListenerCollectionBase>(new ListenerCollection());
			
			systems.Clear();
			systems = RequiredSimulationSystems;
			if (systems == null)
			{
				systems = new List<Type>();
				
				AppSystemResolver.ResolveFor<SimulationApplication>(systems);
			}

			var simulationAppEntity = Global.World.CreateEntity();
			simulationAppEntity.Set<IListener>(new SimulationApplication(Global, null));
			simulationAppEntity.Set(new PushToListenerCollection(listenerCollection));

			foreach (var type in systems)
				RetrieveApplication().Data.Collection.GetOrCreate(type);

			if (RetrieveApplication().Scheduler is Scheduler scheduler)
			{
				scheduler.OnExceptionFound = exception =>
				{
					Console.WriteLine(exception);
					return true;
				};
			}
		}
		
		[TearDown]
		public void TearDown()
		{
			// TODO: Real way for disposing GlobalWorld
			Global.World.Dispose();
		}
	}
}