using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RpcEventCollectionSystem : AppSystem
	{
		public delegate void OnCommandRequest(GameHostCommandResponse response);
		public delegate void OnCommandReply(GameHostCommandResponse response);
		
		public RpcEventCollectionSystem(WorldCollection collection) : base(collection)
		{
		}

		public event OnCommandRequest CommandRequest;
		public event OnCommandRequest CommandReply;

		internal void TriggerCommandRequest(GameHostCommandResponse response)
		{
			CommandRequest?.Invoke(response);
		}
		
		internal void TriggerCommandReply(GameHostCommandResponse response)
		{
			CommandReply?.Invoke(response);
		}
	}
}