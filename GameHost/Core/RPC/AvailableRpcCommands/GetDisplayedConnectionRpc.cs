using System;
using GameHost.Core.Ecs;

namespace GameHost.Core.RPC.AvailableRpcCommands
{
	public class GetDisplayedConnectionRpc : RpcCommandSystem
	{
		public GetDisplayedConnectionRpc(WorldCollection collection) : base(collection)
		{

		}

		public override string CommandId => "displayallcon";

		protected override void OnReceiveRequest(GameHostCommandResponse response)
		{
			Console.WriteLine("received requests!");
		}

		protected override void OnReceiveReply(GameHostCommandResponse response)
		{

		}
	}
}