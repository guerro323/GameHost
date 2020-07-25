using System;
using System.Collections.Generic;
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
			var resource = resourceManager.GetWav(connection, ev.ResourceId);

			entity.Set(resource);
			entity.Set(new PlayAudioRequest());
			entity.Set<StandardAudioPlayerComponent>();
		}

		protected override void OnUpdate()
		{
			var soloud = World.Mgr.Get<Soloud>()[0];
			foreach (var entity in controllerSet.GetEntities())
			{
				if (entity.Has<PlayAudioRequest>())
				{
					entity.Set(soloud.play(entity.Get<Wav>()));
					
					recorder.Record(entity)
					        .Remove<PlayAudioRequest>();
				}
			}
			
			recorder.Execute(World.Mgr);
			
			/*foreach (var entity in controllerSet.GetEntities())
			{
				ref var play = ref entity.Get<SPlay>();

				var isDelayed = entity.Has<SPlay.Delayed>();
				if (play.Delay > TimeSpan.Zero)
				{
					play.Delay = worldTime.Total.Add(play.Delay, worldTime.Delta);
					entity.Set<SPlay.Delayed>();

					isDelayed = true;
				}

				if (!isDelayed || play.Delay <= worldTime.Total)
				{
					// todo: play
					
					recorder.Record(entity)
					        .Dispose();
				}
			}*/
		}
	}
}