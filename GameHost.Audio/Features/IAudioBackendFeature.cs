using GameHost.Applications;

namespace GameHost.Audio.Features
{
	/// <summary>
	/// Represent an audio backend
	/// </summary>
	public interface IAudioBackendFeature : IFeature
	{
		public bool IsLocalized { get; }
	}
}