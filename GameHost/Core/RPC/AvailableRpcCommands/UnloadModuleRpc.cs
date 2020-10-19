using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Modules.Feature;
using Newtonsoft.Json;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public class UnloadModuleRpc : RpcCommandSystem
	{
		public class Result
		{
			public int    ErrorCode   { get; set; }
			public string ErrorString { get; set; }
		}

		private EntitySet moduleSet;
		
		public UnloadModuleRpc(WorldCollection collection) : base(collection)
		{
			moduleSet = collection.Mgr.GetEntities()
			                      .With<RegisteredModule>()
			                      .AsSet();
		}

		public override string CommandId => "unloadmodule";

		protected override void OnReceiveRequest(GameHostCommandResponse response)
		{
			if (response.Data.Length == 0)
				return;
			
			var moduleId = response.Data.ReadString();
			foreach (var entity in moduleSet.GetEntities())
			{
				var m = entity.Get<RegisteredModule>();
				if (m.Description.NameId != moduleId)
					continue;

				if (m.State == ModuleState.Loaded)
				{
					World.Mgr.CreateEntity()
					     .Set(new RequestUnloadModule {Module = entity});

					GetReplyWriter()
						.WriteStaticString(JsonConvert.SerializeObject(new Result
						{
							ErrorCode = 0,
							ErrorString = $"Module '{moduleId}' Unloaded!"
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