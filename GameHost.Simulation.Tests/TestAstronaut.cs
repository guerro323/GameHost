using System;
using System.Numerics;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.Utility.EntityQuery;
using NUnit.Framework;
using StormiumTeam.GameBase.Utility.Misc.EntitySystem;

namespace GameHost.Simulation.Tests
{
	public class AstronautSampleTest
	{
		public struct Position : IComponentData
		{
			public Vector3 Value;
		}

		public struct Velocity : IComponentData
		{
			public Vector3 Value;
		}

		public struct Gravity : IComponentData
		{
			public Vector3 Value;
		}

		public struct Time : IComponentData
		{
			public float Delta;
		}
		
		[SetUp]
		public void Setup()
		{
		}

		[Test]
		public void Test()
		{
			var world      = new GameWorld();
			var timeEntity = world.CreateEntity();
			world.AddComponent(timeEntity, new Time {Delta = 0.25f});

			var gravityEntity    = world.CreateEntity();
			var gravityComponent = world.AddComponent(gravityEntity, new Gravity {Value = {Y = -9.6f}});

			var astronautOnEarth = world.CreateEntity();
			world.AssignComponent(astronautOnEarth, gravityComponent);
			world.AddComponent(astronautOnEarth, new Velocity {Value = new Vector3(10, 0, 0)});
			world.AddComponent<Position>(astronautOnEarth);
			
			var astronautOnMoon = world.CreateEntity();
			world.AddComponent(astronautOnMoon, new Gravity {Value   = {Y = -4}});
			world.AddComponent(astronautOnMoon, new Velocity {Value = new Vector3(10, 0, 0)});
			world.AddComponent<Position>(astronautOnMoon);

			static void onAstronaut(in ReadOnlySpan<GameEntityHandle> entities, in SystemState<GameEntityHandle> state)
			{
				var (gameWorld, timeEntity) = state;

				ref readonly var time = ref gameWorld.GetComponentData<Time>(timeEntity);
				foreach (ref readonly var entity in entities)
				{
					ref readonly var gravity = ref gameWorld.GetComponentData<Gravity>(entity);

					ref var velocity = ref gameWorld.GetComponentData<Velocity>(entity);
					ref var position = ref gameWorld.GetComponentData<Position>(entity);

					velocity.Value += gravity.Value * time.Delta;
					position.Value += velocity.Value * time.Delta;
				}
			}

			var system = new ArchetypeSystem<GameEntityHandle>(onAstronaut, new EntityQuery(world, new []
			{
				world.AsComponentType<Gravity>(),
				world.AsComponentType<Position>(),
				world.AsComponentType<Velocity>()
			}));

			system.Update(timeEntity);

			Console.WriteLine($"{world.GetComponentData<Position>(astronautOnEarth).Value}");
			Console.WriteLine($"{world.GetComponentData<Position>(astronautOnMoon).Value}");
		}
	}
}