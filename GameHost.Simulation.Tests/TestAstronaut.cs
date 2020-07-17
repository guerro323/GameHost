using System;
using System.Numerics;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using NUnit.Framework;

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

			foreach (ref readonly var entity in world.Boards.Entity.Alive)
			{
				if (!world.HasComponent<Gravity>(entity)
				    && !world.HasComponent<Position>(entity)
				    && !world.HasComponent<Velocity>(entity))
					continue;

				ref readonly var gravity = ref world.GetComponentData<Gravity>(entity);

				ref var velocity = ref world.GetComponentData<Velocity>(entity);
				ref var position = ref world.GetComponentData<Position>(entity);

				velocity.Value += gravity.Value * world.GetComponentData<Time>(timeEntity).Delta;
				position.Value += velocity.Value * world.GetComponentData<Time>(timeEntity).Delta;
			}

			Console.WriteLine($"{world.GetComponentData<Position>(astronautOnEarth).Value}");
			Console.WriteLine($"{world.GetComponentData<Position>(astronautOnMoon).Value}");
		}
	}
}