using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Modules.Feature;
using JetBrains.Annotations;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public struct LoadModuleRpc : IGameHostRpcWithResponsePacket<NoMembersResponsePacket>
	{
		public string ModuleId { get; set; }

		[UsedImplicitly]
		public class System : RpcPacketWithResponseSystem<LoadModuleRpc, NoMembersResponsePacket>
		{
			private readonly EntitySet moduleSet;
		
			public System(WorldCollection collection) : base(collection)
			{
				moduleSet = collection.Mgr.GetEntities()
				                      .With<RegisteredModule>()
				                      .AsSet();
			}

			public override string MethodName => "GameHost.LoadModule";
			protected override NoMembersResponsePacket GetResponse(in LoadModuleRpc request)
			{
				foreach (var entity in moduleSet.GetEntities())
				{
					var m = entity.Get<RegisteredModule>();
					if (m.Description.NameId != request.ModuleId)
						continue;

					if (m.State == ModuleState.None)
					{
						World.Mgr.CreateEntity()
						     .Set(new RequestLoadModule {Module = entity});

						return default;
					}
				}

				return WithError(1, $"No Module with ID '{request.ModuleId}' found!");
			}
		}
	}
}