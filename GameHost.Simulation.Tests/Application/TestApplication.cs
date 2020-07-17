using System.Diagnostics;
using GameHost.Core.Execution;
using GameHost.Injection;
using GameHost.Simulation.Application;
using GameHost.Simulation.TabEcs;
using GameHost.Threading;
using GameHost.Worlds;
using NUnit.Framework;

namespace GameHost.Simulation.Tests.Application
{
	public class TestApplication : TestApplicationBase
	{
		[Test]
		public void HasGameWorld()
		{
			var app = RetrieveApplication();

			var gameWorld = new ContextBindingStrategy(app.Data.Context, true).Resolve<GameWorld>();

			Assert.IsTrue(gameWorld != null);
		}

		[Test]
		public void EntitiesCanBeCreated()
		{
			var app = RetrieveApplication();

			var gameWorld = new ContextBindingStrategy(app.Data.Context, true).Resolve<GameWorld>();
			var entity = gameWorld.CreateEntity();
			
			Assert.IsTrue(entity.Id > 0);
		}
	}
}