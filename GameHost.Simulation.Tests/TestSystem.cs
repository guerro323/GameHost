﻿using System;
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
				if (i > 25_000 && i < 60_000)
					gameWorld.AddComponent(entities[i], gameWorld.AsComponentType<Seperation_1>());
				if (i > 50_000 && i < 80_000)
					gameWorld.AddComponent(entities[i], gameWorld.AsComponentType<Seperation_2>());
				if (i > 75_000)
				{
					gameWorld.AddComponent(entities[i], gameWorld.AsComponentType<Seperation_3>());
					if (i > 90_000)
						gameWorld.AddComponent(entities[i], gameWorld.AsComponentType<Seperation_4>());
				}

				if (i < 5_000)
					gameWorld.AddComponent(entities[i], gameWorld.AsComponentType<Seperation_4>());
			}

			var sw     = new Stopwatch();
			var lowest = TimeSpan.MaxValue;

			using var runner = new ThreadBatchRunner(1f);
			for (var i = 0; i < 10_000; i++)
			{
				var query = new EntityQuery(gameWorld, new[] {componentType}); 
				/*var system = new EntitySystemComponent<int, IntComponent>((in GameEntityHandle handle, ref IntComponent component, in SystemState<int> _) =>
				{
					component.Value++;
				}, query);*/
				var system = new ArchetypeSystem<int>((in EntityArchetype _, in ReadOnlySpan<GameEntityHandle> entities, in SystemState<int> state) =>
				{
					var accessor = new ComponentDataAccessor<IntComponent>(state.World);
					foreach (ref readonly var entity in entities)
						accessor[entity].Value++;
				}, query);
				system.PrepareBatch(0);

				sw.Restart();
				var request = runner.Queue(system);
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

			Console.WriteLine($"Elapsed={lowest.TotalMilliseconds}ms");
		}

		public struct IntComponent : IComponentData
		{
			public int Value;
		}
		
		public struct Seperation_1 : IComponentData {}
		public struct Seperation_2 : IComponentData {}
		public struct Seperation_3 : IComponentData {}
		public struct Seperation_4 : IComponentData {}
	}
}