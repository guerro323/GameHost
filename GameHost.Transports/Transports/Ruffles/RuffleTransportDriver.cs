using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.InteropServices;
using GameHost.Core.IO;
using NetFabric.Hyperlinq;
using Ruffles.Channeling;
using Ruffles.Configuration;
using Ruffles.Connections;
using Ruffles.Core;

namespace GameHost.Transports.Transports.Ruffles
{
	public class RuffleTransportAddress : TransportAddress
	{
		public IPEndPoint EndPoint;
		
		public override TransportDriver Connect()
		{
			var driver = new RuffleTransportDriver();
			driver.Connect(EndPoint);
			return driver;
		}
	}
	
	public class RuffleTransportDriver : TransportDriver
	{
		public RuffleSocket socket;

		// 12000 bytes, which should be sufficient
		private byte[]                                      tempBuffer = new byte[12_000];
		
		private Dictionary<TransportConnection, Connection> connectionMapForward;
		private Dictionary<Connection, TransportConnection> connectionMapReverse;

		private List<GCHandle> handleToDealloc;

		private         TransportAddress m_Address;
		public override TransportAddress TransportAddress => m_Address;

		public RuffleTransportDriver()
		{
			connectionMapForward = new Dictionary<TransportConnection, Connection>();
			connectionMapReverse = new Dictionary<Connection, TransportConnection>();

			handleToDealloc = new List<GCHandle>();
		}

		public bool Listen(int port)
		{
			socket = new RuffleSocket(new SocketConfig
			{
				ChallengeDifficulty = 20,
				IPv4ListenAddress   = IPAddress.Parse("0.0.0.0"),
				ChannelTypes = new[]
				{
					ChannelType.ReliableFragmented
				},
				DualListenPort = port
			});

			var r = socket.Start();
			m_Address = new RuffleTransportAddress {EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port)};
			return r;
		}

		public bool Connect(IPEndPoint endPoint)
		{
			socket = new RuffleSocket(new SocketConfig
			{
				ChallengeDifficulty = 20,
				ChannelTypes = new[]
				{
					ChannelType.ReliableSequencedFragmented
				},
				DualListenPort = 0
			});
			
			if (!socket.Start())
				return false;
			
			var con = socket.Connect(endPoint);
			if (con != null)
			{
				return true;
			}

			return false;
		}

		public override TransportConnection       Accept()
		{
			return default;
		}

		private struct QueuedEvent
		{
			public TransportEvent.EType type;
			public TransportConnection  con;
			public ArraySegment<byte>   data;
			public NetworkEvent         origin;
		}

		private Queue<QueuedEvent> queuedEvents = new Queue<QueuedEvent>();
		private uint               lastConId    = 1;

		public override void Update()
		{
			foreach (var handle in handleToDealloc)
			{
				if (!handle.IsAllocated)
					throw new InvalidOperationException("Dealloced handle!");
				handle.Free();
			}
			handleToDealloc.Clear();

			if (socket == null)
				throw new InvalidOperationException("no socket");

			if (queuedEvents.Count > 0)
				throw new NotImplementedException("we have events in this connection");

			NetworkEvent ev;
			while ((ev = socket.Poll()).Type != NetworkEventType.Nothing)
			{
				switch (ev.Type)
				{
					case NetworkEventType.Connect:
					{
						var tCon = new TransportConnection {Id = lastConId++, Version = 1};

						connectionMapForward[tCon]          = ev.Connection;
						connectionMapReverse[ev.Connection] = tCon;
						queuedEvents.Enqueue(new QueuedEvent
						{
							type = TransportEvent.EType.Connect,
							con  = tCon
						});

						break;
					}

					case NetworkEventType.Disconnect:
					case NetworkEventType.Timeout:
					{
						if (connectionMapReverse.TryGetValue(ev.Connection, out var value))
						{
							connectionMapForward.Remove(value);
							connectionMapReverse.Remove(ev.Connection);
							queuedEvents.Enqueue(new QueuedEvent
							{
								type = TransportEvent.EType.Disconnect,
								con  = value
							});
						}

						break;
					}

					case NetworkEventType.Data:
					{
						queuedEvents.Enqueue(new QueuedEvent
						{
							con    = connectionMapReverse[ev.Connection],
							data   = ev.Data,
							origin = ev,
							type   = TransportEvent.EType.Data
						});

						break;
					}
				}

				// for data we recycle later
				if (ev.Type != NetworkEventType.Data)
					ev.Recycle();
			}
		}

		public override TransportEvent PopEvent()
		{
			if (queuedEvents.TryDequeue(out var ev))
			{
				if (ev.type == TransportEvent.EType.Data)
				{
					handleToDealloc.Add(GCHandle.Alloc(ev.data.Array, GCHandleType.Pinned));
					ev.origin.Recycle();
				}
			}
			return new TransportEvent
			{
				Connection = ev.con,
				Type = ev.type,
				Data = ev.data
			};
		}

		public override TransportConnection.State GetConnectionState(TransportConnection con)
		{
			return connectionMapForward[con].State switch
			{
				ConnectionState.Disconnected => TransportConnection.State.Disconnected,
				ConnectionState.Connected => TransportConnection.State.Connected,
				ConnectionState.RequestingConnection => TransportConnection.State.PendingApproval,
				ConnectionState.RequestingChallenge => TransportConnection.State.PendingApproval,
				ConnectionState.SolvingChallenge => TransportConnection.State.Connecting,
				_ => throw new ArgumentOutOfRangeException()
			};
		}

		private uint msgCounter = 0;

		public override int Send(TransportChannel chan, TransportConnection con, Span<byte> data)
		{
			if (data.Length > tempBuffer.Length)
				throw new InvalidOperationException($"{data.Length} bigger than {tempBuffer.Length}!");
			
			var segment = new ArraySegment<byte>(tempBuffer, 0, data.Length);
			data.CopyTo(segment);

			if (!connectionMapForward[con].Send(segment, 0, true, msgCounter++))
				return -1;
			return 0;
		}

		public override int                       Broadcast(TransportChannel             chan, Span<byte>          data)
		{
			throw new NotImplementedException("broadcast not yet implemented (not needed for current usage)");
		}

		public override int                       GetConnectionCount()
		{
			return connectionMapForward.Count;
		}

		public override void GetConnections(Span<TransportConnection> span)
		{
			var i = 0;
			foreach (var (con, _) in connectionMapForward)
			{
				span[i++] = con;
			}
		}

		public override void                      Dispose()
		{
			socket?.Shutdown();
		}
	}
}