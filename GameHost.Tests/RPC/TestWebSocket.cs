using System;
using System.Runtime.InteropServices;
using System.Text.Json;
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
		struct TestValue
		{
			public int Value { get; set; }
		}
		
		[Test]
		public void IsValidJsonNotification()
		{
			var json = @"
{
	""jsonrpc"": ""2.0"",
	""method"": ""test"",
	""params"": {
		""Value"": 4
	}
}
";

			using var document = JsonDocument.Parse(json);
			Assert.IsTrue(document.RootElement.GetProperty("jsonrpc").ValueEquals("2.0"));
			Assert.IsTrue(document.RootElement.GetProperty("method").ValueEquals("test"));
			Assert.IsTrue(document.RootElement.TryGetProperty("params", out var paramsProperty));
			
			Assert.AreEqual(4, JsonSerializer.Deserialize<TestValue>(paramsProperty.ToString()).Value);
		}

		public struct TestNotification : IGameHostRpcPacket
		{
			public int Value { get; set; }
		}
		
		[Test]
		public void Start()
		{
			using (var game = new GameBootstrap())
			{
				game.GameEntity.Set(new GameName("GameTest"));
				game.Global.World.CreateEntity()
				    .Set<IFeature>(new ReceiveGameHostClientFeature(0));
				game.Setup();

				var rpcSystem = game.Global.Collection.GetOrCreate(wc => new RpcSystem(wc));
				rpcSystem.RegisterPacket<TestNotification>("Tests.TestNotification");

				NetManager client = null;
				var        evL    = new RpcListener(game.Global.Collection.GetOrCreate(wc => new RpcLowLevelSystem(wc)));
				evL.PeerConnected += args =>
				{
					rpcSystem.CreateNotification(new TestNotification {Value = 42}, args.clientEntity);
				};
				
				for (var i = 0; i != 128; i++)
				{
					game.Loop();

					if (client == null && game.Global.Collection.TryGet(out StartGameHostListener listener)
					                   && listener.DependencyResolver.Dependencies.Count == 0)
					{
						client = new NetManager(evL);
						client.Start();
						client.Connect("127.0.0.1", listener.Server.Value.LocalPort, string.Empty);
					}

					client?.PollEvents();
					Thread.Sleep(1);
				}
				
				client?.Stop();

				if (game.Global.Collection.TryGet(out StartGameHostListener startGameHostListener))
				{
					Assert.IsEmpty(startGameHostListener.DependencyResolver.Dependencies);
					
					Assert.IsNotNull(startGameHostListener.Server.Value);
					Assert.IsTrue(startGameHostListener.Server.Value.IsRunning);
				}
				else
					Assert.Fail();

				using (var set = game.Global.World.GetEntities()
				                     .With<TestNotification>()
				                     .Without<EntityRpcMultiHandler>()
				                     .AsSet())
				{
					Assert.AreEqual(1, set.Count);
					Assert.AreEqual(42, set.GetEntities()[0].Get<TestNotification>().Value);
				}

				game.CancellationTokenSource.Cancel();
			}

			Assert.Pass();
		}
	}
}