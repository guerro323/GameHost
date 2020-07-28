using GameHost.Core.Ecs.Passes;

namespace GameHost.Inputs.Systems
{
	public class UpdateInputPassRegister : PassRegisterBase<IUpdateInputPass>
	{
		protected override void OnTrigger()
		{
			foreach (var obj in GetObjects())
			{
				obj.OnInputUpdate();
			}
		}
	}

	public interface IUpdateInputPass
	{
		void OnInputUpdate();
	}
}