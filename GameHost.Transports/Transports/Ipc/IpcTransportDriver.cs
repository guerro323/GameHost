using System;
using System.Runtime.InteropServices;
using GameHost.Core.IO;
using SharedMemory;

namespace GameHost.Transports.Transports.Ipc
{
	public class IpcTransportAddress : TransportAddress
	{
		public string QueueName { get; init; }

		public override TransportDriver Connect()
		{
			if (string.IsNullOrEmpty(QueueName))
				throw new InvalidOperationException(nameof(QueueName));

			var driver = new IpcTransportDriver();
			driver.Create(QueueName, true, true);

			return driver;
		}
	}

	public class IpcTransportDriver : TransportDriver
	{
		private TransportAddress transportAddress;

		private CircularBuffer publisher;
		private CircularBuffer subscriber;

		public override TransportAddress TransportAddress => transportAddress;

		public void Create(string queueName, bool canSend = false, bool canReceive = false)
		{
			if (!canSend && !canReceive)
				throw new InvalidOperationException("creating a driver which can't neither send or receive");

			if (canSend) publisher     = new(queueName, 16, 1024 * 128);
			if (canReceive) subscriber = new(queueName);

			transportAddress = new IpcTransportAddress { QueueName = queueName };
		}

		public override TransportConnection Accept()
		{
			return default;
		}

		public override void Update()
		{
		}

		private byte[] buffer = new byte[1024 * 128];

		public override TransportEvent PopEvent()
		{
			if (subscriber == null)
				return default;

			var bytesRead = subscriber.Read(buffer, timeout: 0);
			if (bytesRead <= 0)
				return default;

			TransportEvent ev;
			ev.Type       = TransportEvent.EType.Data;
			ev.Connection = default;
			ev.Data       = buffer.AsSpan(0, buffer.Length);

			return ev;
		}

		public override TransportConnection.State GetConnectionState(TransportConnection con)
		{
			return TransportConnection.State.Connected;
		}

		public override int Send(TransportChannel chan, TransportConnection con, Span<byte> data) => Broadcast(default, data);

		public override unsafe int Broadcast(TransportChannel chan, Span<byte> data)
		{
			if (publisher == null)
				return 1;
			
			publisher.Write((IntPtr)MemoryMarshal.GetReference(data), data.Length, timeout: 0);
			return 0;
		}

		public override int GetConnectionCount()
		{
			return 0;
		}

		public override void GetConnections(Span<TransportConnection> span)
		{
		}

		public override void Dispose()
		{
			subscriber?.Dispose();
			publisher?.Dispose();

			subscriber = null;
			publisher  = null;
		}
	}
}