using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using GameHost.Applications;
using GameHost.Core.Features;
using GameHost.Injection;
using GameHost.Simulation.Features.ShareWorldState;
using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;
using GameHost.Simulation.Tests.Application;
using GameHost.Transports;
using NUnit.Framework;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Simulation.Tests.Feature
{
	public class TestShareFeature : TestApplicationBase
	{
		[Test]
		public void TestFeatureExist()
		{
			var app = RetrieveApplication();

			var featureEntity = app.Data.World.CreateEntity();
			featureEntity.Set<IFeature>(new ShareWorldStateFeature(new ThreadTransportDriver(1)));

			var resolver = new DependencyResolver(app.Scheduler, app.Data.Context);
			resolver.Add<GetFeature<ShareWorldStateFeature>>(FeatureDependencyStrategy.Absolute<ShareWorldStateFeature>(app.Data.Collection));
			resolver.OnComplete(objects =>
			{
				if (objects.First() is GetFeature<ShareWorldStateFeature> features)
				{
					Assert.AreEqual(1, features.Count, "There should have been one feature in GetFeature<T>");
					return;
				}

				Assert.Fail("The first completed object is not valid");
			});

			Global.Loop();
			if (resolver.Dependencies.Count > 0)
				Assert.Fail("The resolver has dependencies that are not finished.");
		}

		[Test]
		public void TestDataIsCorrect()
		{
			TestFeatureExist(); // important!

			var app = RetrieveApplication();

			var gameWorld = new ContextBindingStrategy(app.Data.Context, false).Resolve<GameWorld>();
			var ent       = gameWorld.CreateEntity();
			gameWorld.AddComponent(ent, new IntComponent());

			Global.Loop();
		}

		[Test]
		public unsafe void TestCustomSerializer()
		{
			TestFeatureExist(); // important!

			var app = RetrieveApplication();

			var gameWorld  = new ContextBindingStrategy(app.Data.Context, false).Resolve<GameWorld>();
			var sendSystem = app.Data.Collection.GetOrCreate(c => new SendWorldStateSystem(c));
			var serializer = new CustomIntSerializer();
			sendSystem.SetComponentSerializer(gameWorld.AsComponentType<IntComponent>(), serializer);

			var ent = gameWorld.CreateEntity();
			gameWorld.AddComponent(ent, new IntComponent());

			Global.Loop();

			Assert.AreEqual(serializer.Passed, 1);
		}

		public struct IntComponent : IComponentData
		{
		}

		public class CustomIntSerializer : IShareComponentSerializer
		{
			public int Passed;

			public bool CanSerialize(GameWorld world, Span<GameEntity> entities, ComponentBoardBase board)
			{
				return true;
			}

			public void SerializeBoard(ref DataBufferWriter buffer, GameWorld world, Span<GameEntity> entities, ComponentBoardBase board)
			{
				Passed = 0;
				foreach (var entity in entities)
				{
					if (world.HasComponent<IntComponent>(entity))
						Passed++;
				}
			}
		}
	}
}