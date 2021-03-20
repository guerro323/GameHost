using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Modules.Feature;
using JetBrains.Annotations;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public struct UnloadModuleRpc : IGameHostRpcWithResponsePacket<NoMembersResponsePacket>
	{
		public string ModuleId { get; set; }

		[UsedImplicitly]
		public class System : RpcPacketWithResponseSystem<UnloadModuleRpc, NoMembersResponsePacket>
		{
			private readonly EntitySet moduleSet;

			public System(WorldCollection collection) : base(collection)
			{
				moduleSet = collection.Mgr.GetEntities()
				                      .With<RegisteredModule>()
				                      .AsSet();
			}

			public override string MethodName => "GameHost.UnloadModule";

			protected override NoMembersResponsePacket GetResponse(in UnloadModuleRpc request)
			{
				foreach (var entity in moduleSet.GetEntities())
				{
					var m = entity.Get<RegisteredModule>();
					if (m.Description.NameId != request.ModuleId)
						continue;

					if (m.State == ModuleState.Loaded)
					{
						World.Mgr.CreateEntity()
						     .Set(new RequestUnloadModule {Module = entity});

						return default;
					}
				}

				return WithError(1, $"No Module with ID '{request.ModuleId}' found!");
			}
		}
	}
}