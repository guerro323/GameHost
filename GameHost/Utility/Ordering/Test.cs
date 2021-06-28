using GameHost.Core.Ecs;
using GameHost.Core.Modules.Feature;

namespace GameHost.Utility
{
	public class Test
	{
		public void Add()
		{
			var systemA = 10;
			var systemB = 20;

			var orderGroup = new OrderGroup<int>();

			var systemAUpdater = orderGroup.GetOrCreate(systemA);
			orderGroup.GetOrCreate(systemB);

			if (orderGroup.TryGet(systemB, out var systemBUpdater))
			{
				systemAUpdater.Constraints.Add(systemBUpdater.Exterior.Right);
			}

		}
	}
}