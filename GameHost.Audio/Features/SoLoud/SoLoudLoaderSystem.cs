using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace GameHost.Audio
{
	public class SoLoudLoaderSystem : AppSystemWithFeature<SoLoudBackendFeature>
	{
		private Soloud  soloud;
		private ILogger logger;

		public SoLoudLoaderSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref logger);
		}

		protected override void OnFeatureAdded(Entity entity, SoLoudBackendFeature feature)
		{
			base.OnFeatureAdded(entity, feature);

			if (soloud != null)
			{
				logger.ZLogCritical("A SoLoud object already exist!");
				return;
			}

			soloud = new Soloud();
			soloud.init();
			soloud.setGlobalVolume(0.5f);

			World.Mgr.CreateEntity()
			     .Set(soloud);
		}

		protected override void OnFeatureRemoved(Entity entity, SoLoudBackendFeature feature)
		{
			base.OnFeatureRemoved(entity, feature);

			soloud.deinit();
			soloud = null;
		}
	}
}