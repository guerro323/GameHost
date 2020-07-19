using System;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Core.IO;
using GameHost.Injection;
using GameHost.IO;

namespace GameHost.Core.Modules
{
	public abstract class GameHostModule
	{
		public readonly Entity  Source;
		public readonly Context Ctx;

		/// <summary>
		/// Get the mod storage
		/// </summary>
		public readonly Bindable<IStorage> Storage;

		public readonly DllStorage DllStorage;

		private object bindableProtection = new object();

		public GameHostModule(Entity source, Context ctxParent, GameHostModuleDescription description)
		{
			if (!description.IsNameIdValid())
				throw new InvalidOperationException($"The mod '{description.NameId}' has invalid characters!");

			Source     = source;
			Ctx        = new Context(ctxParent);
			Storage    = new Bindable<IStorage>(protection: bindableProtection);
			DllStorage = new DllStorage(GetType().Assembly);

			var strategy = new ContextBindingStrategy(Ctx, true);
			var storage  = strategy.Resolve<IStorage>();

			if (storage == null)
				throw new NullReferenceException(nameof(storage));

			storage.GetOrCreateDirectoryAsync($"ModuleData/{description.NameId}").ContinueWith(OnRequiredDirectoryFound);
		}

		private void OnRequiredDirectoryFound(Task<IStorage> task)
		{
			if (task.Result == null)
				return;

			Storage.EnableProtection(false, bindableProtection);
			Storage.Value = task.Result;
			Storage.EnableProtection(true, bindableProtection);
		}
	}
}