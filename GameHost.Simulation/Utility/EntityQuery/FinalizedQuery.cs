using System;
using GameHost.Simulation.TabEcs;

namespace GameHost.Simulation.Utility.EntityQuery
{
	public ref struct FinalizedQuery
	{
		public Span<ComponentType> All;
		public Span<ComponentType> None;
	}
}