using GameHost.Simulation.TabEcs;
using GameHost.Simulation.TabEcs.Interfaces;

namespace GameHost.Simulation.Utility.Time
{
	/// <summary>
	/// Represent the current time data of a <see cref="GameWorld"/>
	/// </summary>
	public struct GameTime : IComponentData
	{
		public int    Frame;
		public float  Delta;
		public double Elapsed;
	}
}