using GameHost.Audio.Features;
using GameHost.Core.IO;

namespace GameHost.Audio
{
	public struct SoLoudBackendFeature : IAudioBackendFeature
	{
		public TransportAddress TransportAddress { get; }
		public bool             IsLocalized      => true;

		public SoLoudBackendFeature(TransportAddress address)
		{
			TransportAddress = address;
		}
	}
}