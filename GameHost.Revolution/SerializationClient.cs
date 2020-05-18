using System;

namespace GameHost.Revolution
{
	public struct SerializationClient : IEquatable<SerializationClient>, IComparable<SerializationClient>
	{
		public int CompareTo(SerializationClient other)
		{
			return Id.CompareTo(other.Id);
		}

		public static bool operator <(SerializationClient left, SerializationClient right)
		{
			return left.CompareTo(right) < 0;
		}

		public static bool operator >(SerializationClient left, SerializationClient right)
		{
			return left.CompareTo(right) > 0;
		}

		public static bool operator <=(SerializationClient left, SerializationClient right)
		{
			return left.CompareTo(right) <= 0;
		}

		public static bool operator >=(SerializationClient left, SerializationClient right)
		{
			return left.CompareTo(right) >= 0;
		}

		internal readonly int Id;

		public SerializationClient(int id)
		{
			Id = id;
		}

		public bool Equals(SerializationClient other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is SerializationClient other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Id;
		}
	}
}