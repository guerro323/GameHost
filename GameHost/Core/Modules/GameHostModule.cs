using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Core.IO;
using GameHost.Injection;
using GameHost.IO;

namespace GameHost.Core.Modules
{
	public abstract class GameHostModule : IDisposable
	{
		public readonly Entity  Source;
		public readonly Context Ctx;

		/// <summary>
		/// Get the mod storage
		/// </summary>
		public readonly Bindable<IStorage> Storage;

		public readonly DllStorage DllStorage;

		public readonly ModuleAssemblyLoadContext AssemblyLoadContext;

		private object bindableProtection = new object();

		protected List<IDisposable> ReferencedDisposables;

		public GameHostModule(Entity source, Context ctxParent, GameHostModuleDescription description)
		{
			if (!description.IsNameIdValid())
				throw new InvalidOperationException($"The mod '{description.NameId}' has invalid characters!");

			Source                = source;
			Ctx                   = new Context(ctxParent);
			Storage               = new Bindable<IStorage>(protection: bindableProtection);
			DllStorage            = new DllStorage(GetType().Assembly);
			ReferencedDisposables = new List<IDisposable>();

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

		public void AddDisposable(IDisposable disposable) => ReferencedDisposables.Add(disposable);

		protected virtual void OnDispose()
		{
		}

		public void Dispose()
		{
			OnDispose();

			foreach (var d in ReferencedDisposables)
			{
				try
				{
					d.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}

			ReferencedDisposables = null;

			Storage.Dispose();
			Ctx.Dispose();
		}
	}
}