﻿using System;
using Collections.Pooled;
using ENet;
using GameHost.Core.IO;

namespace GameHost.Transports
{
	public unsafe partial class ENetTransportDriver
	{
		private class Connection : IDisposable
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

			public Connection(in Peer peer)
			{
				m_PeerPtr              = peer.NativeData;
				Id                     = peer.ID;
				m_DataStream           = new PooledList<byte>();
				m_IncomingEvents       = new PooledQueue<DriverEvent>();
				QueuedForDisconnection = false;
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
	}
}