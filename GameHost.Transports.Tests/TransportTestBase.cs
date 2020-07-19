using System;
using System.Threading;
using GameHost.Core.IO;

namespace GameHost.Transports.Tests
{
	public class TransportTestBase
	{
		protected bool DoServerClientTest(TransportDriver server, TransportDriver client)
		{
			var server_event_receivedConnect = false;
			var server_event_receivedData    = false;

			var client_event_receivedConnect = false;
			var client_event_receivedData    = false;

			var ccs = new CancellationTokenSource(TimeSpan.FromSeconds(1));
			while (!ccs.IsCancellationRequested
			       && (!server_event_receivedConnect || !server_event_receivedData)
			       && (!client_event_receivedConnect || !client_event_receivedData))
			{
				server.Update();
				client.Update();

				TransportEvent ev;
				while (server.Accept().IsCreated)
				{
				}

				while ((ev = server.PopEvent()).Type != TransportEvent.EType.None)
				{
					switch (ev.Type)
					{
						case TransportEvent.EType.Connect:
							Console.WriteLine("Server - Received Connect");
							server_event_receivedConnect = true;

							server.Send(default, ev.Connection, new byte[] {42});

							break;
						case TransportEvent.EType.Data:
							Console.WriteLine("Server - Received Data");
							server_event_receivedData = true;
							break;
					}
				}

				while (client.Accept().IsCreated)
				{
				}

				while ((ev = client.PopEvent()).Type != TransportEvent.EType.None)
				{
					switch (ev.Type)
					{
						case TransportEvent.EType.Connect:
							Console.WriteLine("Client - Received Connect");

							client.Send(default, ev.Connection, new byte[] {42});

							client_event_receivedConnect = true;
							break;
						case TransportEvent.EType.Data:
							Console.WriteLine("Client - Received Data");
							client_event_receivedData = true;
							break;
					}
				}
			}

			return (server_event_receivedConnect && server_event_receivedData)
			       && (client_event_receivedConnect && client_event_receivedData);
		}
	}
}