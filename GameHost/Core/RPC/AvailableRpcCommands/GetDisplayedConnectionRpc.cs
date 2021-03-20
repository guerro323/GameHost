using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public struct GetDisplayedConnectionRpc : IGameHostRpcWithResponsePacket<GetDisplayedConnectionRpc.Response>
	{
		public struct Response : IGameHostRpcResponsePacket
		{
			public struct Connection
			{
				public string Name    { get; set; }
				public string Type    { get; set; }
				public string Address { get; set; }
			}

			public Dictionary<string, List<Connection>> Connections { get; set; }
		}

		[UsedImplicitly]
		public class System : RpcPacketWithResponseSystem<GetDisplayedConnectionRpc, Response>
		{
			private EntitySet connectionSet;
			
			public System(WorldCollection collection) : base(collection)
			{
				connectionSet = World.Mgr.GetEntities()
				                     .With<DisplayedConnection>()
				                     .With<TransportAddress>()
				                     .AsSet();
			}

			public override string MethodName => "GameHost.GetDisplayedConnection";
			
			protected override Response GetResponse(in GetDisplayedConnectionRpc request)
			{
				var connectionMap = new Dictionary<string, List<Response.Connection>>();
				foreach (var connection in connectionSet.GetEntities())
				{
					var displayed = connection.Get<DisplayedConnection>();
					if (displayed.ApplicationType == null)
						continue;
				
					if (!connectionMap.TryGetValue(displayed.ApplicationType.Name, out var list))
						connectionMap[displayed.ApplicationType.Name] = list = new();
					list.Add(new()
					{
						Name    = displayed.Name,
						Type    = displayed.Type,
						Address = displayed.EndPoint.ToString()
					});
				}

				return new() {Connections = connectionMap};
			}
		}
	}
}