using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GameHost.Core.Threading;

namespace GameHost.Threading
{
	public abstract class ListenerCollectionBase : IDisposable, IThreadSynchronizer
	{
		public abstract bool CanCallUpdateFromCurrentContext();

		public abstract void                           AddListener(IListener      listener, params IListenerKey[] keys);
		public abstract bool RemoveListener(IListener listener);
		
		public abstract IListener                      RetrieveFirst(IListenerKey key);
		public abstract IReadOnlyCollection<IListener> RetrieveAll(IListenerKey   key);
		public abstract TimeSpan                       Update();

		public          bool IsDisposed { get; protected set; }
		public abstract void Dispose();

		public abstract SynchronizationManager.SyncContext SynchronizeThread(TimeSpan span = default);
	}

	public class ListenerCollection : ListenerCollectionBase, IScheduler
	{
		protected Dictionary<IListenerKey, List<IListener>> ListenersMap = new Dictionary<IListenerKey, List<IListener>>();
		protected List<IListener>                           Listeners    = new List<IListener>();

		public override void AddListener(IListener listener, params IListenerKey[] keys)
		{
			using (SynchronizeThread())
			{
				if (Listeners.Contains(listener))
					throw new Exception("Listener already exists.");
				
				foreach (var key in keys)
				{
					key.ThrowIfNotValid(ListenersMap, listener);
					key.Insert(ListenersMap, listener);
				}

				Listeners.Add(listener);

				listener.OnAttachedToUpdater(this);
			}
		}

		public override bool RemoveListener(IListener toRemove)
		{
			using (SynchronizeThread())
			{
				foreach (var listenerList in ListenersMap.Values)
				{
					while (listenerList.Contains(toRemove))
						listenerList.Remove(toRemove);
				}

				if (Listeners.Remove(toRemove))
				{
					toRemove.OnRemovedFromUpdater(this);
					return true;
				}

				return false;
			}
		}

		public override IListener RetrieveFirst(IListenerKey key)
		{
			using (SynchronizeThread())
			{
				if (!ListenersMap.ContainsKey(key))
					return null;
				return ListenersMap[key].FirstOrDefault();
			}
		}

		public override IReadOnlyCollection<IListener> RetrieveAll(IListenerKey key)
		{
			using (SynchronizeThread())
			{
				if (key == null)
					return Listeners;
				if (!ListenersMap.ContainsKey(key))
					return null;
				return ListenersMap[key];
			}
		}

		public override TimeSpan Update()
		{
			if (IsDisposed)
				return default;
			
			var timeToSleep = TimeSpan.MaxValue;
			using (SynchronizeThread())
			{
				foreach (var listener in Listeners)
				{
					timeToSleep = new TimeSpan(Math.Min(listener.OnUpdate(this).TimeToSleep.Ticks, timeToSleep.Ticks));
				}
			}

			if (timeToSleep == TimeSpan.MaxValue)
				timeToSleep = TimeSpan.FromSeconds(0.01);

			return timeToSleep;
		}

		public override void Dispose()
		{
			IsDisposed = true;
			
			using (SynchronizeThread())
			{
				foreach (var map in ListenersMap)
					map.Value.Clear();
				ListenersMap.Clear();
				ListenersMap = null;

				foreach (var listener in Listeners)
					listener.OnRemovedFromUpdater(this);

				Listeners.Clear();
				Listeners = null;
			}
		}

		public override bool CanCallUpdateFromCurrentContext()
		{
			return true;
		}

		private SynchronizationManager manager = new SynchronizationManager();

		public override SynchronizationManager.SyncContext SynchronizeThread(TimeSpan span = default)
		{
			if (span == default)
				span = TimeSpan.FromSeconds(10);
			return new SynchronizationManager.SyncContext(manager, span);
		}
		
		private Scheduler scheduler = new Scheduler();

		void IScheduler.Run()
		{
			scheduler.Run();
		}

		public void Add<T>(T task) where T : ISchedulerTask
		{
			scheduler.Add(task);
		}

		public void Schedule(Action action, in SchedulingParameters parameters)
		{
			scheduler.Schedule(action, parameters);
		}

		public void Schedule<T>(Action<T> action, T args, in SchedulingParametersWithArgs parameters)
		{
			scheduler.Schedule(action, args, parameters);
		}
	}

	public class ThreadListenerCollection : ListenerCollection
	{
		public readonly Thread Thread;

		private CancellationTokenSource disposeTokenSource;

		public ThreadListenerCollection(string threadName, CancellationToken cancellationToken)
		{
			disposeTokenSource = new CancellationTokenSource();
			
			Thread = new Thread(() =>
			{
				var spin = new SpinWait();
				while (!cancellationToken.IsCancellationRequested && !disposeTokenSource.IsCancellationRequested)
				{
					TimeSpan timeToSleep;
					using (SynchronizeThread())
					{
						timeToSleep = Update();
					}

					timeToSleep *= 0.1f;
					cancellationToken.WaitHandle.WaitOne(timeToSleep);
					if (timeToSleep < TimeSpan.FromMilliseconds(1))
						spin.SpinOnce();
				}
			});
			Thread.Name = threadName;
			Thread.Start();
		}

		public override bool CanCallUpdateFromCurrentContext()
		{
			return false;
		}

		public override void Dispose()
		{
			base.Dispose();
			
			disposeTokenSource.Cancel();
		}
	}
}