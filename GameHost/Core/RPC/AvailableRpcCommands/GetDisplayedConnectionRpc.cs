using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using Newtonsoft.Json;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public struct GetDisplayedConnection : IGameHostRpcWithResponsePacket<GetDisplayedConnection.Response>
	{
		public struct Response : IGameHostRpcResponsePacket
		{
			public struct Connection
			{
				public string Name    { get; set; }
				public string Type    { get; set; }
				public string Address { get; set; }
			}

			public Dictionary<string, Connection> Connections { get; set; }
		}
	}

	public class GetDisplayedConnectionRpc : RpcCommandSystem
	{
		public class Result
		{
			public class Connection
			{
				public string Name { get; set; }
				public string Type           { get; set; }
				public string Address        { get; set; }
			}

			public Dictionary<string, List<Connection>> ConnectionMap { get; set; }
		}

		private EntitySet connectionSet;
		private RpcSystem rpcSystem;

		public GetDisplayedConnectionRpc(WorldCollection collection) : base(collection)
		{
			connectionSet = World.Mgr.GetEntities()
			                     .With<DisplayedConnection>()
			                     .With<TransportAddress>()
			                     .AsSet();
			
			DependencyResolver.Add(() => ref rpcSystem);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			rpcSystem.RegisterPacketWithResponse<GetDisplayedConnection, GetDisplayedConnection.Response>("GameHost.DisplayConnections");
		}

		public override string CommandId => "displayallcon";

		protected override void OnReceiveRequest(GameHostCommandResponse response)
		{
			var connectionMap = new Dictionary<string, List<Result.Connection>>();
			foreach (var connection in connectionSet.GetEntities())
			{
				var displayed = connection.Get<DisplayedConnection>();
				if (displayed.ApplicationType == null)
					continue;
				
				if (!connectionMap.TryGetValue(displayed.ApplicationType.Name, out var list))
					connectionMap[displayed.ApplicationType.Name] = list = new List<Result.Connection>();
				list.Add(new Result.Connection
				{
					Name    = displayed.Name,
					Type    = displayed.Type,
					Address = displayed.EndPoint.ToString()
				});
			}

			var reply = GetReplyWriter();
			reply.WriteStaticString(JsonConvert.SerializeObject(new Result
			{
				ConnectionMap = connectionMap
			}));
			Console.WriteLine("send display");
		}

		protected override void OnReceiveReply(GameHostCommandResponse response)
		{
			// what
		}
	}
}