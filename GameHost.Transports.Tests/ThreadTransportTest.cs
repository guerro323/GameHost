using System;
using System.Threading;
using GameHost.Core.IO;
using NUnit.Framework;

namespace GameHost.Transports.Tests
{
	public class ThreadTransportTest : TransportTestBase
	{
		[Test]
		public void TestCanListen()
		{
			using (var transport = new ThreadTransportDriver(1))
			{
				Assert.NotNull(transport.Listen().Source);
			}
		}

		[Test]
		public void TestClientServer()
		{
			using (var server = new ThreadTransportDriver(32))
			using (var client = new ThreadTransportDriver(1))
			{
				var server_addr = server.Listen();
				Assert.NotNull(server.Listen().Source);

				client.Connect(server_addr);
				Assert.IsTrue(DoServerClientTest(server, client));
			}
		}
	}
}