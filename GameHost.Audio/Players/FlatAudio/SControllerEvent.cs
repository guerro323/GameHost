using System;

namespace GameHost.Audio.Features
{
	public struct SControllerEvent
	{
		public enum EState
		{
			Paused,
			Stop,
			Play
		}

		public EState   State;
		public int      ResourceId;
		public int      Player;
		public float    Volume;
		public TimeSpan Delay;
	}
}