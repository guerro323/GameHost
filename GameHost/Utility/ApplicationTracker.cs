using System;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Modules;
using GameHost.Core.Threading;
using GameHost.Injection;
using GameHost.Threading;
using GameHost.Worlds;

namespace GameHost.Utility
{
	public class ApplicationTracker<T> : IDisposable
		where T : IApplication
	{
		public readonly GlobalWorld GlobalWorld;

		private bool        isDisposed;
		private IDisposable msgOnNewListener;

		public ApplicationTracker(Action<T> onListener, GlobalWorld globalWorld, bool automaticSchedule = true)
		{
			var original = onListener;
			onListener = listener =>
			{
				if (automaticSchedule && listener is IScheduler scheduler)
					scheduler.Schedule(v => original(v), listener, default);
				else
					original(listener);
			};
			
			GlobalWorld = globalWorld;
			msgOnNewListener = GlobalWorld.World.SubscribeComponentAdded((in Entity entity, in IListener value) =>
			{
				if (!(value is T asT))
					return;
				
				GlobalWorld.Scheduler.Schedule(v =>
				{
					if (isDisposed)
						return;

					onListener(v);
				}, asT, default);
			});

			foreach (var listener in GlobalWorld.World.Get<IListener>())
			{
				if (!(listener is T asT))
				{
					continue;
				}
				
				onListener(asT);
			}
		}

		public void Dispose()
		{
			isDisposed = true;

			msgOnNewListener.Dispose();
		}
	}

	public static class ApplicationTracker
	{
		public static IDisposable Track<T>(GameHostModule module, Action<T> onListener, bool automaticSchedule = true)
			where T : IApplication
		{
			return new ApplicationTracker<T>(onListener, new ContextBindingStrategy(module.Ctx, true).Resolve<GlobalWorld>(), automaticSchedule);
		}
	}
}