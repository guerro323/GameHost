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
	
	public enum EMessageInputType
	{
		None = 0,

		// should only be sent by gamehost
		Register = 1,

		// should only be sent by unity
		/// <summary>
		/// Return confirmation of input actions register, to see if they were valid
		/// </summary>
		ReceiveRegister = 2,

		// should only be received by gamehost and sent by unity
		ReceiveInputs = 4,
	}
}