using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using GameHost.Core.IO;
using GameHost.Core.Threading;

namespace GameHost.Transports
{
	/// <summary>
	///     A threaded driver that transport data.
	///     A server will listen and return a <see cref="ListenerAddress" /> that clients can use to connect to.
	/// </summary>
	public partial class ThreadTransportDriver : TransportDriver
	{
		private TransportAddress m_TransportAddress;

		public override TransportAddress TransportAddress => m_TransportAddress;

		private static int MContinuousId = 1;

		private readonly Dictionary<uint, Connection> m_Connections;

		private readonly int[] m_ConnectionVersions;

		private readonly Queue<uint>      m_QueuedConnections;

		private readonly IScheduler scheduler;

		public ThreadTransportDriver(uint maxConnections)
		{
			scheduler = new Scheduler();

			SelfId = (uint) Interlocked.Increment(ref MContinuousId);

			m_ConnectionVersions = new int[maxConnections];
			m_Connections        = new Dictionary<uint, Connection>();
			m_QueuedConnections  = new Queue<uint>();

			for (var i = 0; i != m_ConnectionVersions.Length; i++)
				m_ConnectionVersions[i] = 1;

			Listening = false;
		}

		public uint            SelfId      { get; }
		public ListenerAddress BindAddress { get; private set; }
		public bool            Listening   { get; }

		/// <summary>
		///     Listen to clients.
		/// </summary>
		/// <returns>Return an address used for the client to connect to.</returns>
		public ListenerAddress Listen()
		{
			BindAddress        = new ListenerAddress(this);
			m_TransportAddress = new ThreadTransportAddress(BindAddress);
			return BindAddress;
		}

		private void AddClient(ThreadTransportDriver other)
		{
			lock (m_QueuedConnections)
			{
				AddConnection(new ThreadedPeer {Id = other.SelfId, Source = other});
				m_QueuedConnections.Enqueue(other.SelfId);
			}
		}

		public void Connect(ListenerAddress address)
		{
			lock (m_Connections)
			{
				AddConnection(new ThreadedPeer {Id = address.Source.SelfId, Source = address.Source});
			}

			address.Source.AddClient(this);
		}

		public override void Update()
		{
			// CLEAN
			lock (m_Connections)
			{
				foreach (var connection in m_Connections.Values)
				{
					if (!connection.QueuedForDisconnection && connection.IncomingEventCount > 0)
					{
						TransportEvent.EType ev;
						while ((ev = connection.PopEvent(out _)) != TransportEvent.EType.None)
							Console.WriteLine(ev);
						throw new InvalidOperationException("A connection still had events in queue!");
					}

					while (connection.PopEvent(out var ev) != TransportEvent.EType.None)
					{
					}
				}

				foreach (var connection in m_Connections.Values)
				{
					connection.ResetDataStream();
					if (connection.QueuedForDisconnection) RemoveConnection(connection.Id);
				}

				// UPDATE
				scheduler.Run();
			}
		}

		public override TransportConnection.State GetConnectionState(TransportConnection con)
		{
			lock (m_QueuedConnections)
			lock (m_Connections)
			{
				if (m_QueuedConnections.Contains(con.Id))
					return TransportConnection.State.PendingApproval;
				return m_Connections.ContainsKey(con.Id) ? TransportConnection.State.Connected : TransportConnection.State.Disconnected;
			}
		}

		public override TransportConnection Accept()
		{
			lock (m_QueuedConnections)
			lock (m_Connections)
			{
				if (!m_QueuedConnections.TryDequeue(out var id))
					return default;

				var conSource = m_Connections[id].Peer.Source;
				lock (conSource.m_Connections)
				{
					conSource.scheduler.Schedule(thisConnection => thisConnection.AddEvent(TransportEvent.EType.Connect),
						conSource.m_Connections[SelfId],
						default);
				}

				scheduler.Schedule(foreignCon => foreignCon.AddEvent(TransportEvent.EType.Connect),
					m_Connections[id],
					default);

				return new TransportConnection {Id = id, Version = 1};
			}
		}

		public override unsafe int Send(TransportChannel chan, TransportConnection con, Span<byte> data)
		{
			lock (m_Connections)
			{
				if (!m_Connections.TryGetValue(con.Id, out var connection))
					return -2;

				var otherSource = connection.Peer.Source;
				var dataPtr     = Marshal.AllocHGlobal(data.Length);
				var dataLength  = data.Length;

				data.CopyTo(new Span<byte>((void*) dataPtr, dataLength));

				otherSource.scheduler.Schedule(sd =>
					{
						sd.Connection.AddMessage(sd.Data, sd.Length);
						sd.Dispose();
					},
					new SendData {Connection = connection.Peer.Source.m_Connections[SelfId], Data = dataPtr, Length = dataLength},
					default);

				return 0;
			}
		}

		public override int Broadcast(TransportChannel chan, Span<byte> data)
		{
			lock (m_Connections)
			{
				foreach (var con in m_Connections.Values)
				{
					if (Send(chan, new TransportConnection {Id = con.Id, Version = 1}, data) < 0)
						return -1;
				}
			}

			return 0;
		}

		public override int GetConnectionCount()
		{
			lock (m_Connections)
			{
				return m_Connections.Count - m_QueuedConnections.Count;
			}
		}

		public override void GetConnections(Span<TransportConnection> span)
		{
			lock (m_Connections)
			{
				var i = 0;
				foreach (var connection in m_Connections.Values)
				{
					if (m_QueuedConnections.Contains(connection.Id))
						continue;
					
					span[i++] = new TransportConnection {Id = connection.Id, Version = 1};
				}
			}
		}

		public override TransportEvent PopEvent()
		{
			lock (m_Connections)
			{
				scheduler.Run();
				
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
		}

		private Connection AddConnection(ThreadedPeer peer)
		{
			lock (m_Connections)
			{
				var con = new Connection(peer);
				m_Connections.TryAdd(peer.Id, con);

				return con;
			}
		}

		private void RemoveConnection(uint connectionId)
		{
			lock (m_Connections)
			{
				m_Connections[connectionId].Dispose();
				m_Connections.Remove(connectionId);
			}
		}

		public override void Dispose()
		{
		}

		private struct SendData : IDisposable
		{
			public Connection Connection;
			public IntPtr     Data;
			public int        Length;

			public void Dispose()
			{
				Marshal.FreeHGlobal(Data);
			}
		}
	}
}