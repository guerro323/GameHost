using System;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
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

		protected override void OnFeatureAdded(SoLoudBackendFeature feature)
		{
			base.OnFeatureAdded(feature);

			if (soloud != null)
			{
				logger.ZLogCritical("A SoLoud object already exist!");
				return;
			}

			soloud = new Soloud();
			soloud.init();

			World.Mgr.CreateEntity()
			     .Set(soloud);
		}

		protected override void OnFeatureRemoved(SoLoudBackendFeature feature)
		{
			base.OnFeatureRemoved(feature);

			soloud.deinit();
			soloud = null;
		}
	}
}