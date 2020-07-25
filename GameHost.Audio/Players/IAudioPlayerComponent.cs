using System;

namespace GameHost.Audio.Players
{
	public interface IAudioPlayerComponent
	{
		
	}

	public readonly struct AudioPlayerType
	{
		public readonly Type Type;

		public AudioPlayerType(Type type)
		{
			Type = type;
		}
	}
}