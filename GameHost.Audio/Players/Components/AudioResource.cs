using System;
using DefaultEcs;
using GameHost.Core.IO;
using GameHost.IO;

namespace GameHost.Audio.Players
{
	public class AudioResource : Resource
	{
		public int      Id;
		public TimeSpan Length;
	}

	public struct AudioBytesData
	{
		public byte[] Value;
	}
}