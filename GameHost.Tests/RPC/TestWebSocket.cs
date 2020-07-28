using System;
using System.Threading;
using GameHost.Applications;
using GameHost.Core.Client;
using GameHost.Core.RPC;
using GameHost.Game;
using LiteNetLib;
using LiteNetLib.Utils;
using NUnit.Framework;

namespace GameHost.Tests.RPC
{
	public class TestWebSocket
	{
		[Test]
		public void Start()
		{
			using (var game = new GameBootstrap())
			{
				game.GameEntity.Set(new GameName("GameTest"));
				game.Global.World.CreateEntity()
				    .Set<IFeature>(new ReceiveGameHostClientFeature(0));
				game.Setup();

				NetManager client = null;
				var        evL    = new EventBasedNetListener();
				evL.PeerConnectedEvent += peer =>
				{
					var writer = new NetDataWriter(true);
					writer.Put(nameof(RpcMessageType.Command));
					writer.Put(nameof(RpcCommandType.Send));
					writer.Put("displayallcon");
					peer.Send(writer, DeliveryMethod.ReliableOrdered);
					Console.WriteLine("Sent data");
				};
				
				for (var i = 0; i != 64; i++)
				{
					game.Loop();

					if (client == null && game.Global.Collection.TryGet(out StartGameHostListener listener))
					{
						client = new NetManager(evL);
						client.Start();
						client.Connect("127.0.0.1", listener.Server.LocalPort, string.Empty);
					}
					
					client?.PollEvents();
					Thread.Sleep(1);
				}
				
				client?.Stop();

				if (game.Global.Collection.TryGet(out StartGameHostListener startGameHostListener))
				{
					Assert.IsNotNull(startGameHostListener.Server);
					Assert.IsTrue(startGameHostListener.Server.IsRunning);
				}
				else
					Assert.Fail();

				game.CancellationTokenSource.Cancel();
			}

			Assert.Pass();
		}
	}
}