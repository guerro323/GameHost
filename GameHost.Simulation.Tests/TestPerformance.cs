using System;
using System.Diagnostics;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using NUnit.Framework;

namespace GameHost.Simulation.Tests
{
	public class TestPerformance
	{
		public const int entityCount = 10_000;
		
		[Test]
		public void Test()
		{
			var lowest = TimeSpan.MaxValue;
			for (var iteration = 0; iteration != 100; iteration++)
			{
				var world = new GameWorld();
				var sw    = new Stopwatch();

				var componentType = world.AsComponentType<Component>();
				
				sw.Start();
				for (var i = 0; i != entityCount; i++)
				{
					var ent = world.CreateEntity();
					world.AddComponent(ent, componentType);
					world.RemoveEntity(ent);
				}
				sw.Stop();

				if (lowest > sw.Elapsed)
					lowest = sw.Elapsed;
			}

			Console.WriteLine($"{lowest.TotalMilliseconds}ms");
		}
		
		[Test]
		public void TestBulk()
		{
			var lowest = TimeSpan.MaxValue;
			
			var entities = new GameEntityHandle[entityCount];
			
			for (var iteration = 0; iteration != 100; iteration++)
			{
				var world = new GameWorld();
				var sw    = new Stopwatch();

				var componentType = world.AsComponentType<Component>();
				
				sw.Start();
				
				world.CreateEntityBulk(entities);
				for (var i = 0; i != entityCount; i++)
				{
					var ent = entities[i];
					world.AddComponent(ent, componentType);
					world.RemoveEntity(ent);
				}
				sw.Stop();

				if (lowest > sw.Elapsed)
					lowest = sw.Elapsed;
			}

			Console.WriteLine($"{lowest.TotalMilliseconds}ms");
		}

		public struct Component : IComponentData
		{
		}
	}
}