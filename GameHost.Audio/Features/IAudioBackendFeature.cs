using GameHost.Applications;
using GameHost.Core.IO;

namespace GameHost.Audio.Features
{
	/// <summary>
	/// Represent an audio backend
	/// </summary>
	public interface IAudioBackendFeature : IFeature
	{
		public TransportAddress TransportAddress { get; }
		public bool             IsLocalized      { get; }
	}

	/// <summary>
	/// A feature that send data to <see cref="IAudioBackendFeature"/>
	/// </summary>
	public interface IClientAudioFeature : IFeature
	{
		TransportDriver Driver { get; }

		/// <summary>
		/// Request to a driver
		/// </summary>
		/// <param name="data"></param>
		/// <typeparam name="T"></typeparam>
		void Request<T>(T data);
	}
}