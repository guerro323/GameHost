using System;
using DefaultEcs;
using GameHost.IO;

namespace GameHost.Audio.Players
{
	public static class AudioPlayerUtility
	{
		public static void Initialize<TPlayerBackend>(Entity entity, TPlayerBackend backend = default)
			where TPlayerBackend : IAudioPlayerComponent
		{
			entity.Set(backend);
		}

		public static void SetFireAndForget(Entity entity)
		{
			entity.Set(new AudioFireAndForgetComponent());
		}

		/*public static void SetResource(Entity entity, ResourceHandle<AudioResource> resource)
		{
			entity.Set(resource.Result);
		}*/

		public static void Play(Entity entity)
		{
			entity.Remove<StopAudioRequest>();
			entity.Remove<PauseAudioRequest>();
			entity.Remove<AudioDelayComponent>();

			entity.Set(new PlayAudioRequest());
		}

		public static void Stop(Entity entity)
		{
			entity.Remove<PlayAudioRequest>();
			entity.Remove<PauseAudioRequest>();

			entity.Set(new StopAudioRequest());
		}

		public static void Pause(Entity entity, bool paused)
		{
			entity.Remove<StopAudioRequest>();
			entity.Remove<PlayAudioRequest>();

			entity.Set(new PauseAudioRequest());
		}

		public static void PlayDelayed(Entity entity, TimeSpan delay)
		{
			entity.Remove<StopAudioRequest>();
			entity.Remove<PauseAudioRequest>();

			entity.Set(new AudioDelayComponent(delay));
			entity.Set(new PlayAudioRequest());
		}
	}

	public struct PlayAudioRequest
	{
	}

	public struct PauseAudioRequest
	{
	}

	public struct StopAudioRequest
	{
	}
}