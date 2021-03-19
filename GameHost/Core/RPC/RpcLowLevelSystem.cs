using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RpcLowLevelSystem : AppSystem
	{
		public RpcEventCollection Events;

		private Dictionary<TransportConnection, (Entity ent, RpcClientState state)> conMap = new(32);
		private ILogger                                                             logger;
		private RpcSystem                                                           rpcSystem;
		
		public RpcLowLevelSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref Events);
			DependencyResolver.Add(() => ref logger);
			DependencyResolver.Add(() => ref rpcSystem);
		}

		

		public Entity Connect(TransportConnection con, Action<(RpcClientState state, Entity packet)> sendPacket)
		{
			var state     = new RpcClientState();
			var conEntity = rpcSystem.CreateClient(packet =>
			{
				sendPacket((state, packet));
			});
			conMap[con] = (conEntity, state);

			return conEntity;
		}

		public bool Disconnect(TransportConnection con)
		{
			if (!conMap.TryGetValue(con, out var tuple)) 
				return false;
			
			tuple.ent.Dispose();
			return conMap.Remove(con);

		}

		public void AddResponse(TransportConnection connection, JsonElement methodProperty, JsonElement resultProperty, JsonElement idProperty)
		{
			if (conMap[connection].state.SetResponse(idProperty.GetUInt32(), out var entity))
			{
				
			}
			else
				logger.ZLogWarning($"No request were made with id '{idProperty.GetUInt32()}' but a response from the server was made with it?");
		}

		public void AddRequest(TransportConnection connection, JsonElement methodProperty, JsonElement paramsProperty, JsonElement idProperty)
		{
			Entity followEntity;

			var clientEntity = conMap[connection].ent;

			// It's a notification
			if (idProperty.ValueKind == JsonValueKind.Undefined)
			{
				followEntity = rpcSystem.AddIncomingNotification(methodProperty.GetString(), paramsProperty);
				followEntity.Set(new EntityRpcTargetClient(clientEntity));
				return;
			}

			followEntity = rpcSystem.AddIncomingRequest(methodProperty.GetString(), paramsProperty);
			followEntity.Set(new EntityRpcTargetClient(clientEntity));

			conMap[connection].state.AddRequest(idProperty.GetUInt32(), followEntity);
		}
	}
}