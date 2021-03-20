using System.Collections.Generic;
using DefaultEcs;

namespace GameHost.Core.RPC
{
	public class RpcClientState
	{
		public           uint                     CallWithResponseCount;
		private readonly Dictionary<uint, Entity> clientAwaitingMap = new();

		private readonly Dictionary<uint, Entity> serverAwaitingMap = new();

		public bool SetResponse(uint id, out Entity entity)
		{
			if (clientAwaitingMap.TryGetValue(id, out entity))
			{
				clientAwaitingMap.Remove(id);
				return true;
			}

			return false;
		}

		public uint WaitForResponse(in Entity entity)
		{
			var idToUse = ++CallWithResponseCount;
			clientAwaitingMap[idToUse] = entity;
			return idToUse;
		}

		public void AddRequest(uint replyId, in Entity entity)
		{
			serverAwaitingMap[replyId] = entity;
		}

		public bool FinishReply(uint replyId, out Entity entity)
		{
			if (serverAwaitingMap.TryGetValue(replyId, out entity))
			{
				serverAwaitingMap.Remove(replyId);
				return true;
			}

			return false;
		}
	}
}