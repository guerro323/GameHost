using System;
using System.Diagnostics;
using GameHost.Simulation.TabEcs;
using NUnit.Framework;

namespace GameHost.Simulation.Tests
{
	public class TestArchetype
	{
		[Test]
		public void Test1()
		{
			using var world = new GameWorld();

			var component1 = world.RegisterComponent("Component1", new SingleComponentBoard(sizeof(int), 0));
			var component2 = world.RegisterComponent("Component2", new SingleComponentBoard(sizeof(int), 0));

			GameEntity ent1, ent2, ent3;

			ent1 = world.CreateEntity();
			world.AddComponent(ent1, component1);

			ent2 = world.CreateEntity();
			world.AddComponent(ent2, component1);
			world.AddComponent(ent2, component2);

			ent3 = world.CreateEntity();
			world.AddComponent(ent3, component1);

			Assert.AreEqual(world.GetArchetype(ent1), world.GetArchetype(ent3));
			Assert.AreNotEqual(world.GetArchetype(ent1), world.GetArchetype(ent2));
		}

		[Test]
		public void AddingRemovingPerformance()
		{
			using var world = new GameWorld();

			var component1 = world.RegisterComponent("Component1", new SingleComponentBoard(sizeof(int), 0));

			for (var i = 0; i != 4; i++)
			{
				var entities = new GameEntity[1000];
				world.CreateEntityBulk(entities);

				var sw = new Stopwatch();
				sw.Start();
				foreach (var ent in entities)
					world.AddComponent(ent, component1);
				sw.Stop();
				if (i != 0) 
					Console.WriteLine($"Add -> {sw.Elapsed.TotalMilliseconds}ms");

				sw.Restart();
				foreach (var ent in entities)
					world.RemoveComponent(ent, component1);
				sw.Stop();
				if (i != 0) 
					Console.WriteLine($"Remove -> {sw.Elapsed.TotalMilliseconds}ms");
			}
		}
		
		[Test]
		public void AddingRemovingMultiComponentPerformance()
		{
			using var world = new GameWorld();

			var component1 = world.RegisterComponent("Component1", new SingleComponentBoard(sizeof(int), 0));
			var component2 = world.RegisterComponent("Component2", new SingleComponentBoard(sizeof(int), 0));
			var component3 = world.RegisterComponent("Component3", new SingleComponentBoard(sizeof(int), 0));
			var component4 = world.RegisterComponent("Component4", new SingleComponentBoard(sizeof(int), 0));
			
			for (var i = 0; i != 4; i++)
			{
				var entities = new GameEntity[1000];
				world.CreateEntityBulk(entities);

				var sw = new Stopwatch();
				sw.Start();

				Span<ComponentType> span = stackalloc [] { component1, component2, component3, component4 };
				foreach (var ent in entities)
				{
					world.AddMultipleComponent(ent, span);
				}

				sw.Stop();
				if (i != 0) 
					Console.WriteLine($"Add -> {sw.Elapsed.TotalMilliseconds}ms");

				sw.Restart();
				foreach (var ent in entities)
				{
					world.RemoveMultipleComponent(ent, span);
				}

				sw.Stop();
				if (i != 0) 
					Console.WriteLine($"Remove -> {sw.Elapsed.TotalMilliseconds}ms");
			}
		}
	}
}