using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Core.Modules.Feature;
using Newtonsoft.Json;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public class LoadModuleRpc : RpcCommandSystem
	{
		public class Result
		{
			public int    ErrorCode   { get; set; }
			public string ErrorString { get; set; }
		}

		private EntitySet moduleSet;
		
		public LoadModuleRpc(WorldCollection collection) : base(collection)
		{
			moduleSet = collection.Mgr.GetEntities()
			                      .With<RegisteredModule>()
			                      .AsSet();
		}

		public override string CommandId => "loadmodule";

		protected override void OnReceiveRequest(GameHostCommandResponse response)
		{
			if (response.Data.Length == 0)
				return;
			
			var moduleId = response.Data.ReadString();
			Console.WriteLine(moduleId);
			foreach (var entity in moduleSet.GetEntities())
			{
				var m = entity.Get<RegisteredModule>();
				if (m.Description.NameId != moduleId)
					continue;

				if (m.State == ModuleState.None)
				{
					World.Mgr.CreateEntity()
					     .Set(new RequestLoadModule {Module = entity});

					GetReplyWriter()
						.WriteStaticString(JsonConvert.SerializeObject(new Result
						{
							ErrorCode = 0,
							ErrorString = $"Module '{moduleId}' Loaded!"
						}));
					return;
				}
			}
			
			GetReplyWriter()
				.WriteStaticString(JsonConvert.SerializeObject(new Result
				{
					ErrorCode   = 1,
					ErrorString = $"No Module with ID '{moduleId}' found."
				}));
		}

		protected override void OnReceiveReply(GameHostCommandResponse response)
		{
			// what
		}
	}
}