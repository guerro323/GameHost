using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using DefaultEcs;
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

		public struct TestRequestAdd : IGameHostRpcWithResponsePacket<TestRequestAdd.Response>
		{
			public struct Response : IGameHostRpcResponsePacket
			{
				public int Result { get; set; }
			}

			public int Left  { get; set; }
			public int Right { get; set; }
		}

		class ClientState
		{
			public enum EState
			{
				None,
				Connect,
				Connected,
				HasReply
			}

			public EState                                 State;
			public Entity                                 ClientEntity;
			public RpcCallEntity<TestRequestAdd.Response> Request;
			public RpcCallEntity<TestRequestAdd.Response> Subscribed;

			public int SuccessfulResponse;
		}

		public class ServerState
		{
			public TestNotification? Notification;

			public Entity ServerEntity;
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
				rpcSystem.RegisterPacketWithResponse<TestRequestAdd, TestRequestAdd.Response>("Tests.TestRequestAdd");

				NetManager client = null;
				var        evL    = new RpcListener(game.Global.Collection.GetOrCreate(wc => new RpcLowLevelSystem(wc)));

				var clientState = new ClientState();
				evL.PeerConnected += args =>
				{
					clientState.State        = ClientState.EState.Connect;
					clientState.ClientEntity = args.clientEntity;
				};

				var serverState = new ServerState();
				
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

					if (clientState.State == ClientState.EState.Connect)
					{
						clientState.State = ClientState.EState.Connected;
						rpcSystem.CreateNotification(new TestNotification {Value = 42}, clientState.ClientEntity);

						clientState.Request    = rpcSystem.CreateCall<TestRequestAdd, TestRequestAdd.Response>(new TestRequestAdd {Left = 8, Right = 4}, clientState.ClientEntity);
						clientState.Subscribed = rpcSystem.CreateCall<TestRequestAdd, TestRequestAdd.Response>(new TestRequestAdd {Left = 2, Right = 5}, clientState.ClientEntity);

						Console.WriteLine($"Request: {clientState.Request.Entity}, Subscribed: {clientState.Subscribed.Entity} (client: {clientState.ClientEntity})");
						
						clientState.Subscribed.OnReply += packet =>
						{
							Assert.IsTrue(clientState.Request.Entity.Has<RpcSystem.DestroyOnProcessedTag>());
							
							Assert.AreEqual(7, packet.Value.Result);
							clientState.State = ClientState.EState.HasReply;
							
							clientState.SuccessfulResponse++;
						};
					}

					client?.PollEvents();
					Thread.Sleep(1);

					// If we read the response (and nobody subscribed to the entity) the entity shouldn't be alive after we read it.
					if (clientState.Request.HasResponse)
					{
						Assert.IsTrue(clientState.Request.Entity.Has<RpcSystem.DestroyOnProcessedTag>());
						
						// Doing that will remove the entity
						Assert.AreEqual(12, clientState.Request.Response.Result);
						Assert.IsFalse(clientState.Request.Entity.IsAlive);

						clientState.SuccessfulResponse++;
					}

					// If we had a reply, and that we subscribed on it, the entity shouldn't be alive after the event has been triggered.
					if (clientState.State == ClientState.EState.HasReply)
					{
						Assert.IsFalse(clientState.Subscribed.Entity.IsAlive);
					}

					using (var set = game.Global.World.GetEntities()
					                     .With<TestRequestAdd>()
					                     .With<RpcSystem.ClientRequestTag>()
					                     .AsSet())
					{
						foreach (var entity in set.GetEntities())
						{
							var prepareRpc = rpcSystem.PrepareReply<TestRequestAdd, TestRequestAdd.Response>(entity);
							prepareRpc.ReplyWith(new TestRequestAdd.Response
							{
								Result = prepareRpc.Request.Left + prepareRpc.Request.Right
							});
						}
					}

					using (var set = game.Global.World.GetEntities()
					                     .With<TestNotification>()
					                     .Without<EntityRpcMultiHandler>()
					                     .AsSet())
					{
						if (set.Count == 1)
						{
							serverState.Notification = set.GetEntities()[0].Get<TestNotification>();
						}
					}
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

				Assert.IsFalse(serverState.Notification is null);
				Assert.AreEqual(42, serverState.Notification.Value.Value); // since it's a nullable, we should do Value.Value!
				Assert.AreEqual(2, clientState.SuccessfulResponse);
				
				game.CancellationTokenSource.Cancel();
			}

			Assert.Pass();
		}
	}
}