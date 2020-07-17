using System;

namespace GameHost.Core.IO
{
	public ref struct TransportEvent
	{
		public enum EType : byte
		{
			None              = 0,
			RequestConnection = 9,
			Connect           = 10,
			Disconnect        = 15,
			Data              = 20,
		}

		public EType               Type;
		public TransportConnection Connection;
		public Span<byte>          Data;
	}

	public struct TransportConnection
	{
		public enum State : byte
		{
			Disconnected       = 0,
			Connecting = 5,
			PendingApproval    = 10,
			Connected  = 15
		}

		public uint Id;
		public uint Version;

		public bool IsCreated => Version > 0;
	}

	public struct TransportChannel
	{
		public int Id;
		public int Channel;
	}

	public abstract class TransportDriver : IDisposable
	{
		public abstract TransportEvent            PopEvent();
		public abstract TransportConnection.State GetConnectionState(TransportConnection con);

		public abstract int Send(TransportChannel chan, TransportConnection con, Span<byte> data);

		public abstract void Dispose();
	}
}