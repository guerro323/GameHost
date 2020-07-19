using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Injection;

namespace GameHost.Worlds
{
	public class ApplicationData
	{
		public readonly WorldCollection Collection;
		public          World           World   => Collection.Mgr;
		public          Context         Context => Collection.Ctx;

		public ApplicationData(Context context = null, World world = null)
		{
			Collection = new WorldCollection(context ?? new Context(null), world ?? new World());
		}

		public void Loop()
		{
			Collection.LoopPasses();
		}
	}
}