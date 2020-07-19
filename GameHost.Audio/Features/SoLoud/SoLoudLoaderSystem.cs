﻿using System;
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
		}

		protected override void OnFeatureRemoved(SoLoudBackendFeature feature)
		{
			base.OnFeatureRemoved(feature);

			soloud.deinit();
			soloud = null;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (var feature in Features)
			{
				while (feature.Driver.Accept().IsCreated)
				{}

				TransportEvent ev;
				while ((ev = feature.Driver.PopEvent()).Type != TransportEvent.EType.None)
				{
					switch (ev.Type)
					{
						case TransportEvent.EType.None:
							break;
						case TransportEvent.EType.RequestConnection:
							break;
						case TransportEvent.EType.Connect:
							Console.WriteLine("connection!");
							break;
						case TransportEvent.EType.Disconnect:
							break;
						case TransportEvent.EType.Data:
							Console.WriteLine("data!");
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}
	}
}