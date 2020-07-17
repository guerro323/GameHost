using System;
using GameHost.Core.Ecs;
using GameHost.Core.Features;
using GameHost.Simulation.TabEcs;
using NetFabric.Hyperlinq;

namespace GameHost.Simulation.Features.ShareWorldState
{
	public class SendWorldStateSystem : AppSystem
	{
		private GetFeature<ShareWorldStateFeature> features;
		private GameWorld                          gameWorld;

		public SendWorldStateSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref features, FeatureDependencyStrategy.Absolute<ShareWorldStateFeature>(World));
			DependencyResolver.Add(() => ref gameWorld);
		}

		public override bool CanUpdate() => features.Any() && base.CanUpdate();
	}
}