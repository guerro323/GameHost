using System.Diagnostics;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RpcSystemProcessPackets : AppSystem
	{
		public struct ProcessedTag {}
		
		private EntitySet packetSet;
		private EntitySet packetToDestroySet;
		
		private EntitySet publicClientSet;

		public RpcSystemProcessPackets(WorldCollection collection) : base(collection)
		{
			packetSet = collection.Mgr.GetEntities()
			                      .With<EntityRpcMultiHandler>()
			                      .AsSet();
			
			packetToDestroySet = collection.Mgr.GetEntities()
			                               .With<EntityRpcMultiHandler>()
			                               .With<RpcSystem.DestroyOnProcessedTag>()
			                               .AsSet();

			publicClientSet = collection.Mgr.GetEntities()
			                            .With<EntityRpcPublicClient>()
			                            .With<EntityRpcClientInvokeOnSendPacket>()
			                            .AsSet();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			var clientEntities = publicClientSet.GetEntities();

			foreach (ref readonly var packet in packetSet.GetEntities())
			{
				if (packet.TryGet(out EntityRpcTargetClient target))
					SendPacketTo(packet, target.Client);
				else
				{
					foreach (ref readonly var client in clientEntities)
						SendPacketTo(packet, client);
				}
			}

			packetToDestroySet.DisposeAllEntities();
			packetSet.Set<ProcessedTag>(); // packets that were destroyed will not have this component added
		}

		private void SendPacketTo(Entity packet, Entity client)
		{
			Debug.Assert(client.Has<EntityRpcClientInvokeOnSendPacket>(), "client.Has<EntityRpcClientInvokeOnSendPacket>()");

			var action = client.Get<EntityRpcClientInvokeOnSendPacket>().OnSendPacket;
			Debug.Assert(action != null, "action != null");

			action(packet);
		}
	}
}