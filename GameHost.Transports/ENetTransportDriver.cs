using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using Collections.Pooled;
using ENet;
using GameHost.Core.IO;

namespace GameHost.Transports
{
	public unsafe class ENetTransportDriver : TransportDriver
	{
		/// <summary>
		///     The bind address
		/// </summary>
		private Address m_Address;

		private readonly Dictionary<uint, Connection> m_Connections;

		private readonly int[] m_ConnectionVersions;

		private bool m_DidBind;

		/// <summary>
		///     The sockets that is currently used
		/// </summary>
		private Host m_Host;

		private readonly List<SendPacket> m_PacketsToSend;
		private          int              m_PipelineCount;
		private readonly List<int>        m_PipelineReliableIds;
		private readonly List<int>        m_PipelineUnreliableIds;
		private readonly Queue<uint>      m_QueuedConnections;

		public ENetTransportDriver(uint maxConnections)
		{
			MaxConnections = maxConnections;

			m_Address               = default;
			m_Host                  = new Host();
			m_PacketsToSend         = new List<SendPacket>();
			m_ConnectionVersions    = new int[maxConnections];
			m_Connections           = new Dictionary<uint, Connection>();
			m_QueuedConnections     = new Queue<uint>();
			m_PipelineReliableIds   = new List<int>();
			m_PipelineUnreliableIds = new List<int>();
			m_PipelineCount         = 1;

			for (var i = 0; i != m_ConnectionVersions.Length; i++)
				m_ConnectionVersions[i] = 1;

			Listening = false;
			m_DidBind = false;
		}

		public Host Host           => m_Host;
		public uint MaxConnections { get; }

		public bool IsCreated => Host.IsCreated;

		public bool Listening { get; private set; }

		public override void Dispose()
		{
			if (IsCreated)
			{
				foreach (var connection in m_Connections.Values) connection.Dispose();


				m_Host.Flush();
			}

			m_Host.Dispose(true);
		}

		public void Update()
		{
			// CLEAN
			foreach (var connection in m_Connections.Values)
			{
				if (!connection.QueuedForDisconnection && connection.IncomingEventCount > 0)
					throw new InvalidOperationException("A connection still had events in queue!");

				while (connection.PopEvent(out _) != TransportEvent.EType.None)
				{
				}
			}

			foreach (var connection in m_Connections.Values)
			{
				connection.ResetDataStream();
				if (connection.QueuedForDisconnection) RemoveConnection(connection.Id);
			}

			// UPDATE
			foreach (var info in m_PacketsToSend) info.Peer.Send(info.Channel, info.Packet);

			m_PacketsToSend.Clear();

			Event netEvent;
			var   polled = false;
			while (!polled)
			{
				if (m_Host.CheckEvents(out netEvent) <= 0)
				{
					if (m_Host.Service(0, out netEvent) <= 0)
						break;

					polled = true;
				}

				var peerId                                                                       = (int) netEvent.Peer.ID;
				if (!m_Connections.TryGetValue(netEvent.Peer.ID, out var connection)) connection = AddConnection(netEvent.Peer);

				switch (netEvent.Type)
				{
					case NetEventType.None:
						break;
					case NetEventType.Connect:
						m_QueuedConnections.Enqueue(netEvent.Peer.ID);
						break;
					case NetEventType.Receive:
						connection.AddMessage(netEvent.Packet.Data, netEvent.Packet.Length);
						netEvent.Packet.Dispose();
						break;
					case NetEventType.Disconnect:
					case NetEventType.Timeout:
					{
						connection.AddEvent(TransportEvent.EType.Disconnect);
						connection.QueuedForDisconnection = true;

						// increment version
						var ver = m_ConnectionVersions[peerId];
						ver++;
						m_ConnectionVersions[peerId] = ver;
						break;
					}

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public int Bind(Address address)
		{
			m_Address = address;
			m_DidBind = m_Host.Create(address, (int) MaxConnections, 32);
			if (m_DidBind)
				return 0;
			return -1;
		}

		public int Listen()
		{
			if (!m_DidBind)
				throw new InvalidOperationException("Driver did not bind.");
			if (Listening)
				throw new InvalidOperationException("This driver is already listening.");

			Listening = true;
			return 0;
		}

		public TransportConnection Accept()
		{
			if (!m_QueuedConnections.TryDequeue(out var id))
				return default;

			m_Connections[id].AddEvent(TransportEvent.EType.Connect);
			return new TransportConnection {Id = id, Version = 1};
		}

		public TransportConnection Connect(Address address)
		{
			if (m_DidBind)
				throw new InvalidOperationException("Cant connecting when bind");

			if (!Host.IsCreated)
			{
				var created = m_Host.Create(1, 32);
				if (!created)
					throw new NotImplementedException("Failed to create");
			}

			var peer = m_Host.Connect(address, 32);
			AddConnection(peer);

			return new TransportConnection {Id = peer.ID, Version = 1};
		}

		public int Disconnect(TransportConnection con)
		{
			if (!con.IsCreated)
				return 0;

			if (m_Connections.TryGetValue(con.Id, out var connection))
			{
				connection.Peer.DisconnectLater(0);
				return 0;
			}

			return -1;
		}

		public override TransportConnection.State GetConnectionState(TransportConnection con)
		{
			if (m_Connections.TryGetValue(con.Id, out var connection))
				switch (connection.Peer.State)
				{
					case PeerState.Disconnecting:
					case PeerState.Disconnected:
						return TransportConnection.State.Disconnected;

					case PeerState.ConnectionPending:
						return TransportConnection.State.PendingApproval;

					case PeerState.Connecting:
						return TransportConnection.State.Connecting;

					case PeerState.Connected:
						return TransportConnection.State.Connected;
				}

			return TransportConnection.State.Disconnected;
		}

		public TransportChannel CreateChannel(params Type[] stages)
		{
			var isReliable = false;
			foreach (var pipe in stages)
				if (pipe == typeof(ReliableChannel))
				{
					isReliable = true;
					break;
				}

			if (isReliable) m_PipelineReliableIds.Add(m_PipelineCount);
			else m_PipelineUnreliableIds.Add(m_PipelineCount);

			m_PipelineCount++;

			return new TransportChannel {Id = m_PipelineCount - 1};
		}

		public override int Send(TransportChannel chan, TransportConnection con, Span<byte> data)
		{
			if (!m_Connections.TryGetValue(con.Id, out var connection))
				return -2;

			var packet = new Packet();
			{
				if (m_PipelineReliableIds.Contains(chan.Id))
				{
					packet.Create((IntPtr) Unsafe.AsPointer(ref data.GetPinnableReference()), data.Length, PacketFlags.Reliable);
					m_PacketsToSend.Add(new SendPacket {Packet = packet, Peer = connection.Peer, Channel = (byte) chan.Channel});
					return 0;
				}

				if (chan.Id == default || m_PipelineUnreliableIds.Contains(chan.Id))
				{
					packet.Create((IntPtr) Unsafe.AsPointer(ref data.GetPinnableReference()), data.Length, PacketFlags.None);
					m_PacketsToSend.Add(new SendPacket {Packet = packet, Peer = connection.Peer, Channel = (byte) chan.Channel});
					return 0;
				}
			}

			return -1;
		}

		public override TransportEvent PopEvent()
		{
			foreach (var connection in m_Connections.Values)
			{
				var con = new TransportConnection {Id = connection.Id, Version = 1};
				var ev  = connection.PopEvent(out var span);
				if (ev != TransportEvent.EType.None)
				{
					TransportEvent transportEvent;
					transportEvent.Type       = ev;
					transportEvent.Data       = span;
					transportEvent.Connection = con;

					return transportEvent;
				}
			}

			return default;
		}

		private Connection AddConnection(Peer peer)
		{
			var con = Connection.Create(peer);
			m_Connections.TryAdd(peer.ID, con);
			return con;
		}

		private void RemoveConnection(uint connectionId)
		{
			m_Connections[connectionId].Dispose();
			m_Connections.Remove(connectionId);
		}

		private struct DriverEvent
		{
			public TransportEvent.EType Type;
			public int                  StreamOffset;
			public int                  Length;
		}

		private struct Connection : IDisposable
		{
			private IntPtr m_PeerPtr;

			public Peer Peer
			{
				get => new Peer(m_PeerPtr);
				set
				{
					if (value.ID != Id)
						throw new InvalidOperationException("Can't set a peer with a different 'Id'");
					m_PeerPtr = value.NativeData;
				}
			}

			public uint Id;
			public bool QueuedForDisconnection;

			private PooledQueue<DriverEvent> m_IncomingEvents;
			private PooledList<byte>         m_DataStream;

			public int IncomingEventCount => m_IncomingEvents.Count;

			public static Connection Create(Peer peer)
			{
				Connection connection;
				connection.m_PeerPtr              = peer.NativeData;
				connection.Id                     = peer.ID;
				connection.m_DataStream           = new PooledList<byte>();
				connection.m_IncomingEvents       = new PooledQueue<DriverEvent>();
				connection.QueuedForDisconnection = false;
				return connection;
			}

			public void ResetDataStream()
			{
				m_DataStream.Clear();
			}

			public void AddEvent(TransportEvent.EType type)
			{
				m_IncomingEvents.Enqueue(new DriverEvent {Type = type});
			}

			public void AddMessage(IntPtr data, int length)
			{
				if (data == IntPtr.Zero)
					throw new NullReferenceException();
				if (length < 0)
					throw new IndexOutOfRangeException(nameof(length) + " < 0");

				var prevLen = m_DataStream.Count;
				m_DataStream.AddRange(new ReadOnlySpan<byte>(data.ToPointer(), length));
				m_IncomingEvents.Enqueue(new DriverEvent {Type = TransportEvent.EType.Data, StreamOffset = prevLen, Length = length});
			}

			public TransportEvent.EType PopEvent(out Span<byte> bs)
			{
				bs = default;
				if (m_IncomingEvents.Count == 0)
					return TransportEvent.EType.None;

				var ev                                       = m_IncomingEvents.Dequeue();
				if (ev.Type == TransportEvent.EType.Data) bs = m_DataStream.Span.Slice(ev.StreamOffset, ev.Length);

				return ev.Type;
			}

			public void Dispose()
			{
				m_IncomingEvents.Dispose();
				m_DataStream.Dispose();
			}
		}

		private struct SendPacket
		{
			public Packet Packet;
			public Peer   Peer;
			public byte   Channel;
		}
	}
}