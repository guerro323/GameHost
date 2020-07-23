using GameHost.Simulation.Features.ShareWorldState.BaseSystems;
using GameHost.Simulation.TabEcs.Interfaces;

namespace GameHost.Simulation.Utility.Resource.Components
{
	public struct IsResourceEntity : IComponentData
	{
		public class Register : RegisterGameHostComponentData<IsResourceEntity>
		{}
	}
}