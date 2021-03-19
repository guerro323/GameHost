using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
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
		private int               featureCount;
		private RpcListener       listener;
		private RpcLowLevelSystem lowLevel;
		private RpcSystem         rpcSystem;

		private ILogger logger;

		public Bindable<NetManager> Server;

		// we require an absolute type
		public StartGameHostListener(WorldCollection collection) : base(f => f.GetType() == typeof(ReceiveGameHostClientFeature), collection)
		{
			Server       = new();

			DependencyResolver.Add(() => ref logger);
			DependencyResolver.Add(() => ref lowLevel);
			DependencyResolver.Add(() => ref rpcSystem);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			Server.Value = new NetManager(listener = new (lowLevel));

			base.OnDependenciesResolved(dependencies);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if (Server.Value.IsRunning)
			{
				Server.Value.PollEvents();
			}
		}

		protected override void OnFeatureAdded(Entity entity, ReceiveGameHostClientFeature obj)
		{
			base.OnFeatureAdded(entity, obj);
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

		protected override void OnFeatureRemoved(Entity entity, ReceiveGameHostClientFeature obj)
		{
			base.OnFeatureRemoved(entity, obj);
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
		private RpcLowLevelSystem       lowLevel;
		private Utf8JsonWriter          jsonWriter;
		private ArrayBufferWriter<byte> writtenBytes;

		public event Action<(NetPeer peer, Entity clientEntity)> PeerConnected; 

		public RpcListener(RpcLowLevelSystem lowLevel)
		{
			this.lowLevel = lowLevel;

			jsonWriter = new(writtenBytes = new(512));
		}

		public virtual void OnPeerConnected(NetPeer peer)
		{
			var clientEntity = lowLevel.Connect(ToTransportConnection(peer), args =>
			{
				var (state, packetEntity) = args;
				var handler = packetEntity.Get<EntityRpcMultiHandler>();

				jsonWriter.Reset();
				jsonWriter.WriteStartObject();
				{
					jsonWriter.WriteString("jsonrpc", "2.0");
					jsonWriter.WriteString("method", handler.Send.Method);
					jsonWriter.WritePropertyName("params");
					// StartObject
					{
						handler.Send.Send(packetEntity, jsonWriter);
					}
					// EndObject

					if (packetEntity.Has<RpcSystem.RequireReplyTag>())
						jsonWriter.WriteNumber("id", state.WaitForResponse(packetEntity));
				}
				jsonWriter.WriteEndObject();
				jsonWriter.Flush(); // required

				var dataWriter = new NetDataWriter();
				dataWriter.Put(Encoding.UTF8.GetString(writtenBytes.WrittenSpan));
				peer.Send(dataWriter, DeliveryMethod.ReliableOrdered);
			});

			PeerConnected?.Invoke((peer, clientEntity));
		}

		public virtual void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
		{
			Console.WriteLine($"disconnected --> {disconnectInfo}");
			
			lowLevel.Disconnect(ToTransportConnection(peer));
		}

		public virtual void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
		{
			Console.WriteLine($"{endPoint} --> {socketError}");
		}

		public virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
		{
			using var document = JsonDocument.Parse(reader.GetString());
			var       element  = document.RootElement;

			if (!element.TryGetProperty("jsonrpc", out var jsonRpcProperty))
				throw new InvalidOperationException("no jsonrpc property");
			if (!element.TryGetProperty("method", out var methodProperty))
				throw new InvalidOperationException("no method property");

			var hasResult = element.TryGetProperty("result", out var resultProperty);
			var hasId     = element.TryGetProperty("id", out var idProperty);
			var hasParams = element.TryGetProperty("params", out var paramsProperty);
			
			Debug.Assert(jsonRpcProperty.ValueEquals("2.0"), "jsonRpcProperty.ValueEquals('2.0')");

			switch (hasResult)
			{
				case true when hasParams:
					throw new InvalidOperationException("can't be a request and a response at the same time");
				case true when hasId == false:
					throw new InvalidOperationException("follow-up required but no id present");
			}

			var connection = ToTransportConnection(peer);
			if (hasResult)
			{
				lowLevel.AddResponse(connection, methodProperty, resultProperty, idProperty);
			}
			else
			{
				lowLevel.AddRequest(connection, methodProperty, paramsProperty, idProperty);
			}

			/*var response = new GameHostCommandResponse
			{
				Connection = ToTransportConnection(peer),
				Command    = CharBufferUtility.Create<CharBuffer128>(commandId),
				Data       = new DataBufferReader(reader.GetRemainingBytesSegment())
			};*/
		}

		public virtual void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
		{
		}

		public virtual void OnNetworkLatencyUpdate(NetPeer peer, int latency)
		{
		}

		public virtual void OnConnectionRequest(ConnectionRequest request)
		{
			request.Accept();
		}

		private static TransportConnection ToTransportConnection(NetPeer peer) => new() {Id = (uint) peer.Id, Version = 1};
	}
}