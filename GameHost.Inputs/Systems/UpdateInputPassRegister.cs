using GameHost.Core.Ecs.Passes;

namespace GameHost.Inputs.Systems
{
	public class UpdateInputPassRegister : PassRegisterBase<IUpdateInputPass>
	{
		protected override void OnTrigger()
		{
			foreach (var obj in GetObjects())
			{
				if (obj is IUpdatePass updatePass && !updatePass.CanUpdate())
					continue;
				
				obj.OnInputUpdate();
			}
		}
	}

	public interface IUpdateInputPass
	{
		void OnInputUpdate();
	}
}