namespace GameHost.Core.Ecs.Passes
{
	public class InitializePassRegister : PassRegisterBase<IInitializePass>
	{
		protected override void OnTrigger()
		{
			foreach (var sys in GetObjects())
			{
				sys.OnInit();
			}

			finalObjects.Clear();
		}
	}
	
	public interface IInitializePass : IWorldSystem
	{
		void OnInit();
	}
}