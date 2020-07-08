using System;
using GameHost.Core.Applications;

namespace GameHost.Intercommunication
{
	public class ClientComApplication : ApplicationClientBase
	{
		public ITransport Transport { get; set; }

		public override void Connect()
		{
			if (Transport == null)
				throw new NullReferenceException(nameof(Transport));
		}

		public override void Dispose()
		{
			Transport.Dispose();
		}
	}

	public class ServerComApplication : ApplicationHostBase
	{
		public ITransport Transport { get; set; }

		public override void Listen()
		{
			if (Transport == null)
				throw new NullReferenceException(nameof(Transport));
		}

		public override void Dispose()
		{
			Transport.Dispose();
		}
	}
}