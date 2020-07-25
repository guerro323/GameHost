using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Audio.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Native.Char;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio
{
	[RestrictToApplication(typeof(AudioApplication))]
	public class SoLoudPlayerManager : AppSystem
	{
		private readonly struct Key__ : IEquatable<Key__>
		{
			public readonly TransportConnection Connection;
			public readonly int                 Id;

			public Key__(TransportConnection connection, int id)
			{
				Connection = connection;
				Id         = id;
			}

			public bool Equals(Key__ other)
			{
				return Connection.Equals(other.Connection) && Id == other.Id;
			}

			public override bool Equals(object obj)
			{
				return obj is Key__ other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Connection, Id);
			}

			public static bool operator ==(Key__ left, Key__ right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(Key__ left, Key__ right)
			{
				return !left.Equals(right);
			}
		}

		private Dictionary<Key__, (string str, Entity output)> mapped;
		private Dictionary<CharBuffer128, ReadDelegate> delegates;

		public SoLoudPlayerManager(WorldCollection collection) : base(collection)
		{
			mapped = new Dictionary<Key__, (string str, Entity output)>();
			delegates = new Dictionary<CharBuffer128, ReadDelegate>();
		}

		public unsafe void Register(TransportConnection connection, int id, string type)
		{
			var key = new Key__(connection, id);
			if (mapped.ContainsKey(key))
			{
				throw new InvalidOperationException("already mapped");
			}

			mapped[key] = (type, World.Mgr.CreateEntity());
		}

		public Entity Get(TransportConnection connection, int id)
		{
			var key = new Key__(connection, id);
			return mapped[key].output;
		}

		public delegate void ReadDelegate(TransportConnection connection, ref DataBufferReader reader);

		public void AddListener(string type, ReadDelegate del)
		{
			delegates[CharBufferUtility.Create<CharBuffer128>(type)] = del;
		}

		public ReadDelegate GetDelegate(CharBuffer128 str)
		{
			return delegates.ContainsKey(str) ? delegates[str] : null;
		}
	}
}