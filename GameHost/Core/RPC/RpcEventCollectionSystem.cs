using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RpcEventCollectionSystem : AppSystem
	{
		public RpcCollectionObject Global;
		
		public RpcEventCollectionSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref Global);
		}
	}
}