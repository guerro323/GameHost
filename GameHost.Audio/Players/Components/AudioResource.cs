using DefaultEcs;
using GameHost.Core.IO;
using GameHost.IO;

namespace GameHost.Audio.Players
{
	public class AudioResource : Resource
	{
		public int    Id;
	}

	public struct AudioBytesData
	{
		public byte[] Value;
	}
}