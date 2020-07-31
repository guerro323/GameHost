using GameHost.Core.Ecs;
using GameHost.Injection;

namespace GameHost.Core.RPC
{
	public class RpcCollectionObject : AppObject
	{
		public delegate void OnCommandRequest(GameHostCommandResponse response);

		public delegate void OnCommandReply(GameHostCommandResponse response);

		public RpcCollectionObject(Context context) : base(context)
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