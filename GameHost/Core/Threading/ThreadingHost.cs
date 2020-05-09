using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using GameHost.Core.Applications;

namespace GameHost.Core.Threading
{
    public static class ThreadingHost
    {
        public delegate void OnSynchronizationSuccess<in T>(T host);

        public struct ThreadHost
        {
            public object          Host;
            public CustomSemaphore Semaphore;
            public Thread          Thread;
        }

        public class CustomSemaphore
        {
            public Type Origin;

            public SemaphoreSlim Impl;
            public Thread        Thread;

            // generally used for when there is nesting (if we nest semaphore in the same thread it will result into a dead lock)
            public bool CanRunSemaphore()
            {
                if (Thread == Thread.CurrentThread)
                    return false;
                return true;
            }

            public void Use(Thread source)
            {
                Thread = source;
#if ENABLE_SEMAPHORE_DEBUGGING
                Console.WriteLine($"[SEMAPHORE({Origin.Name})] Used by '{source.Name}'");
#endif
            }

            public void Release()
            {
#if ENABLE_SEMAPHORE_DEBUGGING
                Console.WriteLine($"[SEMAPHORE({Origin.Name})] Released by '{Thread.Name}'");
#endif
                Thread = null;
                Impl.Release();
            }
        }

        public static readonly ConcurrentDictionary<Type, ThreadHost> TypeToThread = new ConcurrentDictionary<Type, ThreadHost>();

        public static T GetListener<T>()
            where T : ApplicationHostBase
        {
            return (T)TypeToThread[typeof(T)].Host;
        }

        public static CustomSemaphore GetSemaphore<T>()
        {
            var semaphore = TypeToThread[typeof(T)].Semaphore;
            if (semaphore == null)
                throw new NullReferenceException();
            return semaphore;
        }

        public static ThreadLocker Synchronize<T>()
        {
            var semaphore = GetSemaphore<T>();
            if (semaphore.CanRunSemaphore())
                semaphore.Impl.Wait();

            return new ThreadLocker(ref semaphore);
        }

        public static async Task<ThreadLocker> SynchronizeAsync<T>()
        {
            var semaphore = GetSemaphore<T>();
            if (semaphore.CanRunSemaphore())
                await semaphore.Impl.WaitAsync();

            return new ThreadLocker(ref semaphore);
        }

        public static async void Synchronize<T, TMetadataOnSuccess, TMetadataOnFail>(Action<TMetadataOnSuccess> onSuccess, TMetadataOnSuccess successData,
                                                                                     Action<TMetadataOnFail>    onFail,    TMetadataOnFail    failData, int timeoutMs = 5000, CancellationToken cc = default)
        {
            var semaphore = GetSemaphore<T>();
            if (semaphore.CanRunSemaphore() == false)
            {
                onSuccess?.Invoke(successData);
                return;
            }

            if (await semaphore.Impl.WaitAsync(timeoutMs, cc))
            {
                semaphore.Use(Thread.CurrentThread);
                try
                {
                    onSuccess?.Invoke(successData);
                }
                finally
                {
                    semaphore.Release();
                }
            }

            else
                onFail?.Invoke(failData);
        }

        public static async void Synchronize<T>(OnSynchronizationSuccess<T> onSuccess, Action onFail, int timeoutMs = 5000, CancellationToken cc = default) 
            where T : ApplicationHostBase
        {
            var semaphore = GetSemaphore<T>();
            if (semaphore.CanRunSemaphore() == false)
            {
                onSuccess?.Invoke(GetListener<T>());
                return;
            }

            if (await semaphore.Impl.WaitAsync(timeoutMs, cc))
            {
                semaphore.Use(Thread.CurrentThread);
                try
                {
                    onSuccess?.Invoke(GetListener<T>());
                }
                finally
                {
                    semaphore.Release();
                }
            }
            else
                onFail?.Invoke();
        }
    }

    public abstract class ThreadingHost<T> : ApplicationHostBase
    {
        private Thread                  thread;
        private CancellationTokenSource cts;

        private Scheduler scheduler;

        public Thread    GetThread()    => thread;
        public Scheduler GetScheduler()
        {
            if (thread == null)
                throw new NullReferenceException($"scheduler was null since thread <{typeof(T)}> did not start.");
            return scheduler;
        }

        public bool IsListening { get; private set; }

        public CancellationToken CancellationToken => cts.Token;

        public override void Dispose()
        {
            if (IsListening)
                cts.Cancel();

            thread = null;
        }

        public virtual void ListenOnThread(Thread wantedThread)
        {
            if (IsListening)
                throw new InvalidOperationException("Already listening");

            if (ThreadingHost.TypeToThread.ContainsKey(typeof(T)))
                throw new InvalidOperationException($"No Thread with type '{typeof(T).Name}' should exist when calling 'ApplicationHostThreading.ListenOnThread()'");

            ThreadingHost.TypeToThread[typeof(T)] = new ThreadingHost.ThreadHost {Thread = thread = wantedThread, Host = this, Semaphore = new ThreadingHost.CustomSemaphore {Origin = typeof(T), Impl = new SemaphoreSlim(1, 1)}};
            
            cts         = new CancellationTokenSource();
            IsListening = true;

            scheduler = new Scheduler(thread);
        }

        public override void Listen()
        {
            ListenOnThread(new Thread(OnThreadStart));
            thread.Name = $"{typeof(T).Name}'s thread";
            scheduler   = new Scheduler(thread);
            thread.Start();
        }

        public ThreadLocker SynchronizeThread() => ThreadingHost.Synchronize<T>();

        protected abstract void OnThreadStart();
    }

    public struct ThreadLocker : IDisposable
    {
        public readonly ThreadingHost.CustomSemaphore Semaphore;
        public readonly bool                          DoRelease;

        public ThreadLocker(ref ThreadingHost.CustomSemaphore semaphore)
        {
            DoRelease = semaphore.Thread != Thread.CurrentThread;

            semaphore.Use(Thread.CurrentThread);

            Semaphore = semaphore;
        }

        public void Dispose()
        {
            if (DoRelease)
            {
                Semaphore.Release();
            }
        }
    }
}
