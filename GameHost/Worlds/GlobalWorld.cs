using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Injection;

namespace GameHost.Worlds
{
	public class GlobalWorld
	{
		public readonly WorldCollection Collection;
		public          World           World   => Collection.Mgr;
		public          Context         Context => Collection.Ctx;

		public readonly IScheduler Scheduler;

		public GlobalWorld(Context context = null, World world = null)
		{
			Collection = new WorldCollection(context ?? new Context(null), world ?? new World());
			Scheduler = new Scheduler();
			
			Context.BindExisting(Scheduler);
			Context.BindExisting(this);
		}

		public void Loop()
		{
			Scheduler.Run();
			Collection.LoopPasses();
		}
	}
}