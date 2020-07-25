using System;
using DefaultEcs;
using GameHost.Audio.Players;

namespace GameHost.Audio.Features
{
	public struct SControllerEvent
	{
		public enum EState
		{
			Paused,
			Play
		}

		public EState   State;
		public int      ResourceId;
		public int      Player;
		public float    Volume;
		public TimeSpan Delay;
	}
}