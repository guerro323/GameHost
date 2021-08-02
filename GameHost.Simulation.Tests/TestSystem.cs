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

			// make sure that the runner is warmed up or else the main threads would finish more tasks
			while (!runner.IsWarmed())
			{
			}

			var query = new EntityQuery(gameWorld, new[] {componentType});
			var system = new ArchetypeSystem<int>((in ReadOnlySpan<GameEntityHandle> entities, in SystemState<int> state) =>
			{
				var accessor = new ComponentDataAccessor<IntComponent>(state.World);
				foreach (ref readonly var entity in entities)
					accessor[entity].Value++;
			}, query);
				
			system.PrepareData(0);

			for (var i = 0; i < 5000; i++)
			{
				var request = runner.Queue(system);
				sw.Restart();
				{
					runner.WaitForCompletion(request, true);
				}
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