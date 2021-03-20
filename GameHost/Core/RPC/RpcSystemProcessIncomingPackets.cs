using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RpcSystemProcessIncomingPackets : AppSystem
	{
		private readonly EntitySet garbageNotificationSet;

		public RpcSystemProcessIncomingPackets(WorldCollection collection) : base(collection)
		{
			garbageNotificationSet = World.Mgr.GetEntities()
			                              .With<RpcSystem.NotificationTag>()
			                              .With<RpcSystem.DestroyOnProcessedTag>()
			                              .AsSet();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			garbageNotificationSet.DisposeAllEntities();
		}
	}
}