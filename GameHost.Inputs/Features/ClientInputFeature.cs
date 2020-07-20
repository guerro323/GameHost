using GameHost.Applications;
using GameHost.Core.IO;

namespace GameHost.Inputs.Features
{
	public class ClientInputFeature : IFeature
	{
		public TransportDriver  Driver           { get; }
		public TransportChannel PreferredChannel { get; }

		public ClientInputFeature(TransportDriver driver, TransportChannel channel)
		{
			Driver           = driver;
			PreferredChannel = channel;
		}
	}
}