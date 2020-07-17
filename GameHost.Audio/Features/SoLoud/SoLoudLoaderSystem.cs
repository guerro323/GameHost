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

		protected override void OnFeatureAdded(SoLoudBackendFeature obj)
		{
			base.OnFeatureAdded(obj);

			if (soloud != null)
			{
				logger.ZLogCritical("A SoLoud object already exist!");
				return;
			}

			soloud = new Soloud();
			soloud.init();
		}

		protected override void OnFeatureRemoved(SoLoudBackendFeature obj)
		{
			base.OnFeatureRemoved(obj);

			soloud.deinit();
			soloud = null;
		}
	}
}