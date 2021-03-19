using System.Text.Json;
using System.Threading.Tasks;

namespace GameHost.Core.RPC
{
	public interface IGameHostRpcPacket
	{
	}

	public interface IGameHostRpcWithResponsePacket<TResponse> : IGameHostRpcPacket
		where TResponse : IGameHostRpcResponsePacket
	{
	}

	public interface IGameHostRpcResponsePacket : IGameHostRpcPacket
	{
		
	}

	public struct GetDisplayedConnectionRpc : IGameHostRpcWithResponsePacket<GetDisplayedConnectionRpc.Response>
	{
		public struct Response : IGameHostRpcResponsePacket
		{
		}
	}
}