using System;
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