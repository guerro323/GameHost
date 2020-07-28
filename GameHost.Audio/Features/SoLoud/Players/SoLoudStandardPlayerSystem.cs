using System;
using System.Collections.Generic;
using System.Threading;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Applications;
using GameHost.Audio.Applications;
using GameHost.Audio.Features;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using GameHost.Worlds.Components;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Utility.Misc;

namespace GameHost.Audio.Players
{
	[RestrictToApplication(typeof(AudioApplication))]
	public class SoLoudStandardPlayerSystem : AppSystemWithFeature<SoLoudBackendFeature>
	{
		private SoLoudResourceManager resourceManager;
		private SoLoudPlayerManager playerManager;
		
		private IManagedWorldTime     worldTime;
		
		private EntitySet controllerSet;
		private EntityCommandRecorder recorder;

		public SoLoudStandardPlayerSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref playerManager);
			DependencyResolver.Add(() => ref resourceManager);
			DependencyResolver.Add(() => ref worldTime);

			AddDisposable(recorder = new EntityCommandRecorder());
			AddDisposable(controllerSet = collection.Mgr.GetEntities()
			                                    .With<StandardAudioPlayerComponent>()
			                                    .AsSet());
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);
			playerManager.AddListener(TypeExt.GetFriendlyName(typeof(StandardAudioPlayerComponent)), OnRead);
		}

		public void OnRead(TransportConnection connection, ref DataBufferReader reader)
		{
			var ev       = reader.ReadValue<SControllerEvent>();
			var entity   = playerManager.Get(connection, ev.Player);
			switch (ev.State)
			{
				case SControllerEvent.EState.Paused:
					break;
				case SControllerEvent.EState.Stop:
					entity.Set(new StopAudioRequest());
					break;
				case SControllerEvent.EState.Play:
					var resource = resourceManager.GetWav(connection, ev.ResourceId);

					entity.Set(resource);
					entity.Set(new PlayAudioRequest());
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			entity.Set<StandardAudioPlayerComponent>();
			
			if (ev.Delay > TimeSpan.Zero)
				entity.Set(new AudioDelayComponent(worldTime.Total + ev.Delay - worldTime.Delta));
		}

		protected override void OnUpdate()
		{
			var soloud = World.Mgr.Get<Soloud>()[0];
			foreach (var entity in controllerSet.GetEntities())
			{
				if (entity.Has<PlayAudioRequest>())
				{
					if (!entity.TryGet(out AudioDelayComponent delay) || worldTime.Total >= delay.Delay)
					{
						if (entity.TryGet(out uint currSoloudId))
							soloud.stop(currSoloudId);

						entity.Set(soloud.play(entity.Get<Wav>()));

						recorder.Record(entity)
						        .Remove<PlayAudioRequest>();
						recorder.Record(entity)
						        .Remove<AudioDelayComponent>();
					}
				}

				if (entity.Has<StopAudioRequest>())
				{
					if (!entity.TryGet(out AudioDelayComponent delay) || worldTime.Total >= delay.Delay)
					{
						if (entity.TryGet(out uint currSoloudId))
							soloud.stop(currSoloudId);

						recorder.Record(entity)
						        .Remove<StopAudioRequest>();
						recorder.Record(entity)
						        .Remove<AudioDelayComponent>();
					}
				}
			}

			recorder.Execute(World.Mgr);
		}
	}
}