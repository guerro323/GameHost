using System;
using System.Linq;
using GameHost.Applications;
using GameHost.Core.Features;
using GameHost.Injection;
using GameHost.Simulation.Features.ShareWorldState;
using GameHost.Simulation.Tests.Application;
using NUnit.Framework;

namespace GameHost.Simulation.Tests.Feature
{
	public class TestShareFeature : TestApplicationBase
	{
		[Test]
		public void TestFeatureExist()
		{
			var app = RetrieveApplication();

			var featureEntity = app.Data.World.CreateEntity();
			featureEntity.Set<IFeature>(new ShareWorldStateFeature(null));

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
	}
}