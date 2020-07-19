using GameHost.Core.IO;

namespace GameHost.Transports
{
	public class ThreadTransportAddress : TransportAddress
	{
		public readonly ThreadTransportDriver.ListenerAddress Address;

		public ThreadTransportAddress(ThreadTransportDriver.ListenerAddress address)
		{
			this.Address = address;
		}

		public override TransportDriver Connect()
		{
			var driver = new ThreadTransportDriver(1);
			driver.Connect(Address);
			return driver;
		}
	}
}