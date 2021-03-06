﻿using GameHost.Core.IO;

namespace GameHost.Transports
{
	public partial class ThreadTransportDriver
	{
		public struct ListenerAddress
		{
			public readonly ThreadTransportDriver Source;

			public ListenerAddress(ThreadTransportDriver source)
			{
				Source = source;
			}
		}

		private struct DriverEvent
		{
			public TransportEvent.EType Type;
			public int                  StreamOffset;
			public int                  Length;
		}
	}
}