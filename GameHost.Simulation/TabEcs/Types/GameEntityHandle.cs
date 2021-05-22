using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace GameHost.Simulation.TabEcs
{
	public readonly struct GameEntityHandle : IEquatable<GameEntityHandle>
	{
		public static bool operator ==(GameEntityHandle left, GameEntityHandle right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GameEntityHandle left, GameEntityHandle right)
		{
			return !left.Equals(right);
		}

		// Special syntax, return the first non-default entity.
		public static GameEntityHandle operator |(GameEntityHandle left, GameEntityHandle right)
		{
			if (left != default)
				return left;
			return right;
		}

		// Special syntax, return the last non-default entity.
		public static GameEntityHandle operator &(GameEntityHandle left, GameEntityHandle right)
		{
			if (right != default)
				return right;
			return left;
		}

		public readonly uint Id;

		public GameEntityHandle(uint id)
		{
			Id = id;
		}

		public bool Equals(GameEntityHandle other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is GameEntityHandle other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}

		public override string ToString()
		{
			return $"(GameEntityHandle Row={Id})";
		}
	}

	public readonly struct GameEntity : IEquatable<GameEntity>
	{
		public static bool operator ==(GameEntity left, GameEntity right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GameEntity left, GameEntity right)
		{
			return !left.Equals(right);
		}

		// Special syntax, return the first non-default entity.
		public static GameEntity operator |(GameEntity left, GameEntity right)
		{
			if (left != default)
				return left;
			return right;
		}

		// Special syntax, return the last non-default entity.
		public static GameEntity operator &(GameEntity left, GameEntity right)
		{
			if (right != default)
				return right;
			return left;
		}

		public readonly uint Id;
		public readonly uint Version;

		[JsonConstructor]
		public GameEntity(uint id, uint version)
		{
			Id      = id;
			Version = version;
		}

		public bool Equals(GameEntity other)
		{
			return Id == other.Id && Version == other.Version;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, Version);
		}

		public override bool Equals(object obj)
		{
			return obj is GameEntity other && Equals(other);
		}

		public override string ToString()
		{
			return $"(GameEntity Row={Id} Ver={Version})";
		}


		public GameEntityHandle Handle
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new(Id);
		}
	}
}