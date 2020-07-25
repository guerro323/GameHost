using GameHost.Core.Ecs.Passes;

namespace GameHost.Core.Ecs
{
	public abstract class SystemGroup<T> : AppSystem
	{
		private InitializePassRegister initializePassRegister;
		private UpdatePassRegister     updatePassRegister;

		public SystemGroup(WorldCollection collection) : base(collection)
		{
			initializePassRegister = new InitializePassRegister();
			updatePassRegister     = new UpdatePassRegister();
		}
	}
}