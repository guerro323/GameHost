using System;
using GameHost.Applications;
using GameHost.Injection;
using GameHost.Simulation.TabEcs;
using GameHost.Threading;
using GameHost.Threading.Apps;
using GameHost.Worlds;

namespace GameHost.Simulation.Application
{
	public class SimulationApplication : CommonApplicationThreadListener
	{
		public SimulationApplication(GlobalWorld source, Context overrideContext) : base(source, overrideContext)
		{
			// register game world since it's kinda important for the simu app, ahah
			Data.Context.BindExisting(new GameWorld());
		}

		protected override ListenerUpdate OnUpdate()
		{
			return base.OnUpdate();
		}
	}
}