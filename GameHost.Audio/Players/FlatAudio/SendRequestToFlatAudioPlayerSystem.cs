using System;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Applications;
using GameHost.Audio.Players;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;

namespace GameHost.Audio.Features
{
	public struct SPlay
	{
		public Entity   Resource;
		public Entity   Player;
		public float    Volume;
		public TimeSpan Delay;
		
		public struct Delayed {}
	}

	public class SendRequestToFlatAudioPlayerSystem : AppSystem
	{
		public SendRequestToFlatAudioPlayerSystem(WorldCollection collection) : base(collection)
		{
		}

		private EntitySet playAudioSet;
		private EntitySet toDisposeSet;

		protected override void OnInit()
		{
			base.OnInit();

			var baseSet = World.Mgr.GetEntities()
			                   .With<AudioResourceComponent>()
			                   .With<FlatAudioPlayerComponent>()
			                   .With<PlayAudioRequest>();
			playAudioSet = baseSet.AsSet();
			toDisposeSet = baseSet.With<AudioFireAndForgetComponent>().AsSet();
		}

		protected override void OnUpdate()
		{
			foreach (ref readonly var entity in playAudioSet.GetEntities())
			{
				var volume = 1f;
				if (entity.TryGet(out AudioVolumeComponent volumeComponent))
					volume = volumeComponent.Volume;

				var delay = TimeSpan.Zero;
				if (entity.TryGet(out AudioDelayComponent delayComponent))
					delay = delayComponent.Delay;

				var request = World.Mgr.CreateEntity();
				request.Set(new SPlay
				{
					Resource = entity.Get<AudioResourceComponent>().Source,
					Player   = entity,
					Volume   = volume,
					Delay    = delay
				});
				request.Set<ClientAudioFeature.SendRequest>();
			}

			playAudioSet.Remove<PlayAudioRequest>();
			toDisposeSet.DisposeAllEntities();
		}
	}

	/*public class FlatAudioPlayerSystem : AppSystemWithFeature<IAudioBackendFeature>
	{
		private EntitySet entitySet;
		private EntityCommandRecorder recorder;
		private IManagedWorldTime worldTime;

		public FlatAudioPlayerSystem(WorldCollection collection) : base(collection)
		{
			entitySet = collection.Mgr.GetEntities()
			                      .With<SPlay>()
			                      .AsSet();
			
			recorder = new EntityCommandRecorder();
			
			DependencyResolver.Add(() => ref worldTime);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (var entity in entitySet.GetEntities())
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
			}
		}
	}*/
}