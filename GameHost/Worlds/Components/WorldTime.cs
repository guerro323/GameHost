using System;

namespace GameHost.Worlds.Components
{
	public class ManagedWorldTime : IManagedWorldTime
	{
		public TimeSpan Total { get; set; }
		public TimeSpan Delta { get; set; }
		
		public WorldTime ToStruct()
		{
			return new WorldTime {Total = Total, Delta = Delta};
		}
	}

	public interface IManagedWorldTime
	{
		TimeSpan Total { get; }
		TimeSpan Delta { get; }

		public WorldTime ToStruct()
		{
			return new WorldTime {Total = Total, Delta = Delta};
		}
	}

	public struct WorldTime
	{
		public TimeSpan Total;
		public TimeSpan Delta;
	}
}