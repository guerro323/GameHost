using System;
using GameHost.Applications;
using GameHost.Core.IO;

namespace GameHost.Audio.Features
{
	/// <summary>
	/// Represent an audio backend
	/// </summary>
	public interface IAudioBackendFeature : IFeature
	{
		public TransportDriver  Driver           { get; }
		public TransportAddress TransportAddress { get; }
		public bool             IsLocalized      { get; }
	}

	/// <summary>
	/// A feature that send data to <see cref="IAudioBackendFeature"/>
	/// </summary>
	public class ClientAudioFeature : IFeature
	{
		public TransportDriver  Driver           { get; }
		public TransportChannel PreferredChannel { get; }

		public ClientAudioFeature(TransportDriver driver, TransportChannel channel)
		{
			Driver = driver;
			PreferredChannel = channel;
		}

		public readonly struct SendRequest
		{
		}
	}
}