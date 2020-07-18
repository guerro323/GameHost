using System;
using GameHost.Core.IO;

namespace GameHost.Transports
{
	/// <summary>
	/// A threaded driver that transport data.
	/// A server will listen and return a <see cref="ListenerAddress"/> that clients can use to connect to.
	/// </summary>
	public class ThreadTransportDriver : TransportDriver
	{
		/// <summary>
		/// Listen to clients.
		/// </summary>
		/// <returns>Return an address used for the client to connect to.</returns>
		public ListenerAddress Listen()
		{
			return new ListenerAddress(this);
		}
		
		public override TransportEvent PopEvent()
		{
			
		}

		public override TransportConnection.State GetConnectionState(TransportConnection con)
		{
			
		}

		public override int Send(TransportChannel chan, TransportConnection con, Span<byte> data)
		{
			
		}

		public override void Dispose()
		{
			
		}

		public struct ListenerAddress
		{
			public readonly ThreadTransportDriver Source;

			public ListenerAddress(ThreadTransportDriver source)
			{
				Source = source;
			}
		}
	}
}