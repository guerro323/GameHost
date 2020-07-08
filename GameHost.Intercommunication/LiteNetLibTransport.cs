using System.Collections.Generic;
using System.Net;
using LiteNetLib;
using LiteNetLib.Layers;

namespace GameHost.Intercommunication
{
	public class LiteNetLibTransport : ITransport
	{
		private struct Event
		{
			public byte[]       Data;
			public ReceiveEvent Type;
		}

		public void Dispose()
		{

		}

		private readonly EventBasedNetListener listener;
		private readonly NetManager            manager;

		private readonly Queue<Event> events;

		public LiteNetLibTransport(PacketLayerBase packetLayerBase = null)
		{
			events = new Queue<Event>(8);

			listener                       =  new EventBasedNetListener();
			listener.PeerConnectedEvent    += peer => events.Enqueue(new Event {Type                   = ReceiveEvent.Connect});
			listener.PeerDisconnectedEvent += (peer, info) => events.Enqueue(new Event {Type           = ReceiveEvent.Disconnect});
			listener.NetworkReceiveEvent   += (peer, reader, method) => events.Enqueue(new Event {Data = reader.RawData, Type = ReceiveEvent.Message});

			manager = new NetManager(listener, packetLayerBase);
		}

		public void Connect(IPEndPoint ep)
		{
			manager.Connect(ep, string.Empty);
		}

		public void Listen(int port)
		{
			manager.Start(port);
		}

		public void Send(byte[] data)
		{
			manager.SendToAll(data, DeliveryMethod.ReliableOrdered);
		}

		public bool TryReceive(out byte[] data, out ReceiveEvent type)
		{
			manager.PollEvents();
			if (events.TryDequeue(out var ev))
			{
				data = ev.Data;
				type = ev.Type;
				return true;
			}

			data = null;
			type = ReceiveEvent.None;
			return false;
		}
	}
}