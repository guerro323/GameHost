using System;
using System.Diagnostics;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using NUnit.Framework;

namespace GameHost.Simulation.Tests
{
	public class TestPerformance
	{
		[Test]
		public void Test()
		{
			for (var iteration = 0; iteration != 2; iteration++)
			{
				var world = new GameWorld();
				var sw    = new Stopwatch();
				sw.Start();

				var componentType = world.GetComponentType<Component>();
				for (var i = 0; i != 100_000; i++)
				{
					var ent = world.CreateEntity();
					world.AddComponent(ent, componentType);
					world.RemoveEntity(ent);
				}

				sw.Stop();

				Console.WriteLine(sw.Elapsed.TotalMilliseconds);
			}
		}

		public struct Component : IComponentData
		{
		}
	}
}