using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using RevolutionSnapshot.Core.Buffers;

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