using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.HLAPI;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.Utility.EntityQuery;
using NUnit.Framework;
using StormiumTeam.GameBase.Utility.Misc.EntitySystem;

namespace GameHost.Simulation.Tests
{
	public class TestSystem
	{
		[Test]
		public void TestMillionEntitySystem()
		{
			const int size = 100_000;

			var gameWorld = new GameWorld();
			var entities  = new GameEntityHandle[size];
			gameWorld.CreateEntityBulk(entities);

			var componentType = gameWorld.AsComponentType<IntComponent>();
			for (var i = 0; i < size; i++)
			{
				gameWorld.AddComponent(entities[i], componentType);
			}

			var sw     = new Stopwatch();
			var lowest = TimeSpan.MaxValue;

			using var runner = new ThreadBatchRunner(1f);
			runner.StartPerformanceCriticalSection();

			var query = new EntityQuery(gameWorld, new[] {componentType});
			var system = new ArchetypeSystem<int>((in ReadOnlySpan<GameEntityHandle> entities, in SystemState<int> state) =>
			{
				var accessor = new ComponentDataAccessor<IntComponent>(state.World);
				foreach (ref readonly var entity in entities)
					accessor[entity].Value++;
			}, query);
				
			system.PrepareData(0);
			
			Thread.Sleep(10); // make sure that threads are correctly initialized and are in critical context
			for (var i = 0; i < 1000; i++)
			{
				var request = runner.Queue(system);
				sw.Restart();
				while (!runner.IsCompleted(request))
				{
				}

				/*var accessor = new ComponentDataAccessor<IntComponent>(gameWorld);
				foreach (var archetype in query.Archetypes)
				{
					foreach (ref readonly var entity in MemoryMarshal.Cast<uint, GameEntityHandle>(gameWorld.Boards.Archetype.GetEntities(archetype)))
						accessor[entity].Value++;
				}*/
				
				sw.Stop();

				if (lowest > sw.Elapsed)
					lowest = sw.Elapsed;
				Thread.Sleep(0);
			}
			
			runner.StopPerformanceCriticalSection();

			Console.WriteLine($"Elapsed={lowest.TotalMilliseconds}ms");
		}

		public struct IntComponent : IComponentData
		{
			public int Value;
		}
	}
}