using GameHost.Audio.Features;

namespace GameHost.Audio
{
	public struct SoLoudBackendFeature : IAudioBackendFeature
	{
		public bool IsLocalized => true;
	}
}