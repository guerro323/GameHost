namespace GameHost.Core.Ecs.Passes
{
	public class UpdatePassRegister : PassRegisterBase<IUpdatePass>
	{
		protected override void OnTrigger()
		{
			foreach (var obj in GetObjects())
			{
				if (obj.CanUpdate())
					obj.OnUpdate();
			}
		}
	}
	
	public interface IUpdatePass : IWorldSystem
	{
		bool CanUpdate();
		void OnUpdate();
	}
}