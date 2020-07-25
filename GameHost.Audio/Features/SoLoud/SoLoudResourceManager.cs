using System;
using System.Collections.Generic;
using GameHost.Audio.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;

namespace GameHost.Audio
{
	[RestrictToApplication(typeof(AudioApplication))]
	public class SoLoudResourceManager : AppSystem
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

		private Dictionary<Key__, Wav> mapped;

		public SoLoudResourceManager(WorldCollection collection) : base(collection)
		{
			mapped = new Dictionary<Key__, Wav>();
		}

		public unsafe void Register(TransportConnection connection, int id, Span<byte> span)
		{
			var key = new Key__(connection, id);
			if (mapped.ContainsKey(key))
			{
				throw new InvalidOperationException("already mapped");
			}

			var wav = new Wav();
			fixed (byte* dataPtr = &span.GetPinnableReference())
			{
				wav.loadMem((IntPtr) dataPtr, (uint) span.Length, aCopy: 1);
			}

			mapped[key] = wav;
		}

		public Wav GetWav(TransportConnection connection, int id)
		{
			var key = new Key__(connection, id);
			return mapped[key];
		}
	}
}