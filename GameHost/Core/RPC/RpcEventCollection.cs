using GameHost.Core.Ecs;
using GameHost.Injection;

namespace GameHost.Core.RPC
{
	public class RpcEventCollection : AppObject
	{
		public delegate void OnCommandRequest(GameHostCommandResponse response);

		public delegate void OnCommandReply(GameHostCommandResponse response);

		public RpcEventCollection(Context context) : base(context)
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