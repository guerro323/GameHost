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
}