using System;
using System.Runtime.CompilerServices;
using DefaultEcs;
using DefaultEcs.Command;
using GameHost.Applications;
using GameHost.Audio.Players;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.IO;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Utility.Misc;

namespace GameHost.Audio.Features
{
	public class SendRequestStandardPlayerSystem : AppSystemWithFeature<AudioClientFeature>
	{
		public SendRequestStandardPlayerSystem(WorldCollection collection) : base(collection)
		{
		}

		private EntitySet playAudioSet;
		private EntitySet toDisposeSet;

		private string typeName;

		protected override void OnInit()
		{
			base.OnInit();

			var baseSet = World.Mgr.GetEntities()
			                   .With<ResourceHandle<AudioResource>>()
			                   .With<AudioPlayerId>()
			                   .With<StandardAudioPlayerComponent>()
			                   .With<PlayAudioRequest>();
			playAudioSet = baseSet.AsSet();
			toDisposeSet = baseSet.With<AudioFireAndForgetComponent>().AsSet();

			typeName = TypeExt.GetFriendlyName(typeof(StandardAudioPlayerComponent));
		}

		protected override void OnUpdate()
		{
			foreach (ref readonly var entity in playAudioSet.GetEntities())
			{
				var resource = entity.Get<ResourceHandle<AudioResource>>();
				if (!resource.IsLoaded)
					continue;

				var volume = 1f;
				if (entity.TryGet(out AudioVolumeComponent volumeComponent))
					volume = volumeComponent.Volume;

				var delay = TimeSpan.Zero;
				if (entity.TryGet(out AudioDelayComponent delayComponent))
					delay = delayComponent.Delay;

				using var writer = new DataBufferWriter(16 + Unsafe.SizeOf<SControllerEvent>());
				writer.WriteValue((int) EAudioSendType.SendAudioPlayerData);
				writer.WriteStaticString(typeName);
				writer.WriteValue(new SControllerEvent
				{
					State      = SControllerEvent.EState.Play,
					ResourceId = entity.Get<ResourceHandle<AudioResource>>().Result.Id,
					Player     = entity.Get<AudioPlayerId>().Id,
					Volume     = volume,
					Delay      = delay
				});

				foreach (var feature in Features)
				{
					unsafe
					{
						feature.Driver.Broadcast(feature.PreferredChannel, new Span<byte>((void*) writer.GetSafePtr(), writer.Length));
					}
				}
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