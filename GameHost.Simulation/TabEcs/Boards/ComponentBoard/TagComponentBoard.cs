using System;

namespace GameHost.Simulation.TabEcs
{
	public class TagComponentBoard : ComponentBoardBase
	{
		internal static class Default<T>
		{
			[ThreadStatic]
			public static T V;
		}
		
		public TagComponentBoard(int capacity) : base(0, capacity)
		{
		}
	}
}