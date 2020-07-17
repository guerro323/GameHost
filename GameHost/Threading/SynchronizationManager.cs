using System;
using System.Threading;

namespace GameHost.Threading
{
	public class SynchronizationManager
	{
		public SpinLock Lock;
		public Thread CurrentThread;

		public SynchronizationManager()
		{
			Lock = new SpinLock(true);
		}

		public struct Context : IDisposable
		{
			public readonly SynchronizationManager Synchronizer;
			public readonly bool                   LockTaken;

			public Context(SynchronizationManager synchronizer, TimeSpan timeout)
			{
				Synchronizer = synchronizer;

				if (Synchronizer.CurrentThread == Thread.CurrentThread)
				{
					//Console.WriteLine($"[thread={Thread.CurrentThread.Name}] Lock not taken since recusion");
					LockTaken = false;
					return;
				}

				Synchronizer.CurrentThread = Thread.CurrentThread;

				LockTaken = false;
				Synchronizer.Lock.TryEnter(timeout, ref LockTaken);
				
				//Console.WriteLine($"[thread={Thread.CurrentThread.Name}] Lock taken");
			}

			public void Dispose()
			{
				if (LockTaken)
				{
					Synchronizer.CurrentThread = null;
					Synchronizer.Lock.Exit(true);
					
					//Console.WriteLine($"[thread={Thread.CurrentThread.Name}] Unlock");
				}
			}
		}
	}

	public interface IThreadSynchronizer
	{
		SynchronizationManager.Context SynchronizeThread(TimeSpan span = default);
	}
}