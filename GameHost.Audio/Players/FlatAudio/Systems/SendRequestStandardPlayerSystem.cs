using System;
using System.Runtime.CompilerServices;
using DefaultEcs;
using GameHost.Audio.Players;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.IO;
using GameHost.Worlds.Components;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Utility.Misc;

namespace GameHost.Audio.Features
{
	public class SendRequestStandardPlayerSystem : AppSystemWithFeature<AudioClientFeature>
	{
		public SendRequestStandardPlayerSystem(WorldCollection collection) : base(collection)
		{
		}

		private IManagedWorldTime worldTime;
		
		private EntitySet playerSet;
		private EntitySet playAudioSet;
		private EntitySet stopAudioSet;
		private EntitySet toDisposeSet;

		private string typeName;

		protected override void OnInit()
		{
			base.OnInit();

			var baseSet = new Func<EntityRuleBuilder>(() => World.Mgr.GetEntities()
			                   .With<AudioPlayerId>()
			                   .With<StandardAudioPlayerComponent>());

			playerSet    = baseSet().AsSet();
			playAudioSet = baseSet().With<ResourceHandle<AudioResource>>()
			                      .With<PlayAudioRequest>()
			                      .AsSet();
			stopAudioSet = baseSet().With<StopAudioRequest>().AsSet();
			toDisposeSet = baseSet().With<AudioFireAndForgetComponent>().AsSet();

			typeName = TypeExt.GetFriendlyName(typeof(StandardAudioPlayerComponent));
			
			DependencyResolver.Add(() => ref worldTime);
		}

		protected override void OnUpdate()
		{
			foreach (ref readonly var entity in playerSet.GetEntities())
			{
				if (entity.TryGet(out AudioStartTime startTime))
				{
					entity.Set(new AudioCurrentPlayTime(worldTime.Total - startTime.Value));
				}
			}

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
				
				entity.Set(new AudioStartTime {Value = worldTime.Total + delay});

				foreach (var (_, feature) in Features)
				{
					unsafe
					{
						feature.Driver.Broadcast(feature.PreferredChannel, new Span<byte>((void*) writer.GetSafePtr(), writer.Length));
					}
				}
			}
			
			foreach (ref readonly var entity in stopAudioSet.GetEntities())
			{
				var delay = TimeSpan.Zero;
				if (entity.TryGet(out AudioDelayComponent delayComponent))
					delay = delayComponent.Delay;

				using var writer = new DataBufferWriter(16 + Unsafe.SizeOf<SControllerEvent>());
				writer.WriteValue((int) EAudioSendType.SendAudioPlayerData);
				writer.WriteStaticString(typeName);
				writer.WriteValue(new SControllerEvent
				{
					State  = SControllerEvent.EState.Stop,
					Player = entity.Get<AudioPlayerId>().Id,
					Delay  = delay
				});
				
				foreach (var (_, feature) in Features)
				{
					unsafe
					{
						feature.Driver.Broadcast(feature.PreferredChannel, new Span<byte>((void*) writer.GetSafePtr(), writer.Length));
					}
				}
			}

			playAudioSet.Remove<PlayAudioRequest>();
			playAudioSet.Remove<StopAudioRequest>();
			toDisposeSet.DisposeAllEntities();
		}
	}

	public struct AudioStartTime
	{
		public TimeSpan Value;
	}
}