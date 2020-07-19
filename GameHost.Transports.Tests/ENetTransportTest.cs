using System;
using System.Threading;
using ENet;
using GameHost.Core.IO;
using NUnit.Framework;

namespace GameHost.Transports.Tests
{
	public class ENetTransportTest : TransportTestBase
	{
		[Test]
		public void TestCanListen()
		{
			using (var transport = new ENetTransportDriver(16))
			{
				var addr = new Address {Port = 0};

				Assert.AreEqual(0, transport.Bind(addr));
				Assert.AreEqual(0, transport.Listen());
			}
		}

		[Test]
		public void TestClientServer()
		{
			using (var server = new ENetTransportDriver(16))
			using (var client = new ENetTransportDriver(1))
			{
				var server_addr = new Address {Port = 5983};
				server_addr.SetIP("127.0.0.1");

				Assert.AreEqual(0, server.Bind(server_addr));
				Assert.AreEqual(0, server.Listen());

				var client_servercon = client.Connect(server_addr);
				Assert.IsTrue(client_servercon.IsCreated);

				Assert.IsTrue(DoServerClientTest(server, client));
			}
		}
	}
}