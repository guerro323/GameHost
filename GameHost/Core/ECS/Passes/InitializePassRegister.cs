using System.Collections.Generic;

namespace GameHost.Core.Ecs.Passes
{
	public class InitializePassRegister : PassRegisterBase<IInitializePass>
	{
		protected override void OnTrigger()
		{
			foreach (var sys in GetObjects())
			{
				sys.OnInit();
				sys.HasBeenInitialized = true;
			}

			finalObjects.Clear();
		}

		protected override void OnRegisterCollectionAndFilter(IEnumerable<object> collection)
		{
			temporaryObjects.Clear();
			foreach (var obj in collection)
			{
				if (obj is IInitializePass actOn && !actOn.HasBeenInitialized)
					temporaryObjects.Add(actOn);
			}
		}
	}

	public interface IInitializePass : IWorldSystem
	{
		public bool HasBeenInitialized { get; set; }

		void OnInit();
	}
}