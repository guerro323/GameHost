using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using GameHost.Core.RPC;
using GameHost.Native.Char;
using LiteNetLib;
using LiteNetLib.Utils;
using Microsoft.Extensions.Logging;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.Client
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class StartGameHostListener : AppSystemWithFeature<ReceiveGameHostClientFeature>
	{
		private int                      featureCount;
		private RpcListener              listener;
		private RpcEventCollectionSystem collectionSystem;

		private ILogger logger;

		public Bindable<NetManager> Server;

		// we require an absolute type
		public StartGameHostListener(WorldCollection collection) : base(f => f.GetType() == typeof(ReceiveGameHostClientFeature), collection)
		{
			Server = new Bindable<NetManager>();
			
			DependencyResolver.Add(() => ref logger);
			DependencyResolver.Add(() => ref collectionSystem);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			Server.Value = new NetManager(listener = new RpcListener(collectionSystem));

			base.OnDependenciesResolved(dependencies);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (Server.Value.IsRunning)
				Server.Value.PollEvents();
		}

		protected override void OnFeatureAdded(ReceiveGameHostClientFeature obj)
		{
			base.OnFeatureAdded(obj);
			if (featureCount++ != 0)
				return;

			if (obj.ServerPort > 0)
				Server.Value.Start(obj.ServerPort);
			else
			{
				Server.Value.Start(GetAvailablePort(9000));
			}

			logger.Log(LogLevel.Information, "RPC Server started on port: " + Server.Value.LocalPort);
		}

		protected override void OnFeatureRemoved(ReceiveGameHostClientFeature obj)
		{
			base.OnFeatureRemoved(obj);
			featureCount--;
			if (featureCount == 0)
				Server.Value.Stop();
		}

		public void SendReply(TransportConnection connection, CharBuffer128 command, DataBufferWriter data)
		{
			var peer = Server.Value.GetPeerById((int) connection.Id);
			if (peer == null)
				throw new InvalidOperationException($"Peer '{connection.Id}' not existing");

			var writer = new NetDataWriter(true, data.Length);
			writer.Put(nameof(RpcMessageType.Command));
			writer.Put(nameof(RpcCommandType.Reply));
			writer.Put(command.ToString());
			writer.Put(data.Span.ToArray());

			peer.Send(writer, DeliveryMethod.ReliableOrdered);
		}

		// https://stackoverflow.com/a/45384984
		private static int GetAvailablePort(int startingPort)
		{
			var portArray = new List<int>();

			var properties = IPGlobalProperties.GetIPGlobalProperties();

			// Ignore active connections
			var connections = properties.GetActiveTcpConnections();
			portArray.AddRange(from n in connections
			                   where n.LocalEndPoint.Port >= startingPort
			                   select n.LocalEndPoint.Port);

			// Ignore active tcp listners
			var endPoints = properties.GetActiveTcpListeners();
			portArray.AddRange(from n in endPoints
			                   where n.Port >= startingPort
			                   select n.Port);

			// Ignore active UDP listeners
			endPoints = properties.GetActiveUdpListeners();
			portArray.AddRange(from n in endPoints
			                   where n.Port >= startingPort
			                   select n.Port);

			portArray.Sort();

			for (var i = startingPort; i < UInt16.MaxValue; i++)
				if (!portArray.Contains(i))
					return i;

			return 0;
		}
	}

	public class RpcListener : INetEventListener
	{
		private RpcEventCollectionSystem collection;

		public RpcListener(RpcEventCollectionSystem collection)
		{
			this.collection = collection;
		}

		public virtual void OnPeerConnected(NetPeer peer)
		{
			Console.WriteLine("connected");
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
		{
		}

		public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
		{
		}

		public virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
		{
			Console.WriteLine("received data");
			
			var type = reader.GetString();
			switch (type)
			{
				case nameof(RpcMessageType.Command):
				{
					var commandType = reader.GetString();
					var commandId   = reader.GetString();

					var response = new GameHostCommandResponse
					{
						Connection = new TransportConnection {Id = (uint) peer.Id, Version = 1},
						Command    = CharBufferUtility.Create<CharBuffer128>(commandId),
						Data       = new DataBufferReader(reader.GetRemainingBytesSegment())
					};
					switch (commandType)
					{
						case nameof(RpcCommandType.Send):
						{
							collection.TriggerCommandRequest(response);
							break;
						}
						case nameof(RpcCommandType.Reply):
						{
							collection.TriggerCommandReply(response);
							break;
						}
					}

					break;
				}
			}
		}

		public virtual void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
		{
		}

		public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency)
		{
		}

		public virtual void OnConnectionRequest(ConnectionRequest request)
		{
			Console.WriteLine("accept");
			request.Accept();
		}
	}
}