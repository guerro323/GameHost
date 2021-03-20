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
		
		private ILogger                                                             logger;
		private RpcSystem                                                           rpcSystem;

		public RpcLowLevelSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref Events);
			DependencyResolver.Add(() => ref logger);
			DependencyResolver.Add(() => ref rpcSystem);
		}



		public Entity CreateConnection(Action<(RpcClientState state, Entity packet)> sendPacket)
		{
			var state     = new RpcClientState();
			var conEntity = rpcSystem.CreateClient(packet => { sendPacket((state, packet)); });
			conEntity.Set(state);
			
			return conEntity;
		}

		public bool DestroyConnection(Entity entity)
		{
			if (!entity.IsAlive)
				return false;
			entity.Dispose();
			return true;
		}

		public void AddResponse(Entity connection, JsonElement methodProperty, JsonElement resultProperty, JsonElement idProperty)
		{
			if (connection.Get<RpcClientState>().SetResponse(idProperty.GetUInt32(), out var entity))
			{
				rpcSystem.AddIncomingResponse(entity, methodProperty.GetString(), resultProperty, connection);
			}
			else
				logger.ZLogWarning($"No request were made with id '{idProperty.GetUInt32()}' but a response from the server was made with it?");
		}

		public void AddRequest(Entity connection, JsonElement methodProperty, JsonElement paramsProperty, JsonElement idProperty)
		{
			Entity followEntity;

			// It's a notification
			if (idProperty.ValueKind == JsonValueKind.Undefined)
			{
				followEntity = rpcSystem.AddIncomingNotification(methodProperty.GetString(), paramsProperty, connection);
				return;
			}

			followEntity = rpcSystem.AddIncomingRequest(methodProperty.GetString(), paramsProperty, connection);

			var id = idProperty.GetUInt32();
			connection.Get<RpcClientState>().AddRequest(id, followEntity);
			followEntity.Set(new FollowUpId(id));
		}

		public readonly struct FollowUpId
		{
			public readonly uint Value;

			public FollowUpId(uint id) => Value = id;
		}
	}
}