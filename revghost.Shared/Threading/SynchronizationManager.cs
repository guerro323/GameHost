namespace revghost.Shared.Threading;

public class SynchronizationManager
{
    public SpinLock Lock;

    public SynchronizationManager()
    {
        Lock = new SpinLock(true);
    }

    public SyncContext Synchronize(TimeSpan timeout = default)
    {
        if (timeout == TimeSpan.Zero)
            timeout = TimeSpan.FromSeconds(2);

        return new SyncContext(this, timeout);
    }

    public struct SyncContext : IDisposable
    {
        public readonly SynchronizationManager Synchronizer;
        public readonly bool LockTaken;

        public SyncContext(SynchronizationManager synchronizer, TimeSpan timeout)
        {
            Synchronizer = synchronizer;

            if (Synchronizer.Lock.IsHeldByCurrentThread)
            {
                LockTaken = false;
                return;
            }

            LockTaken = false;
            Synchronizer.Lock.TryEnter(timeout, ref LockTaken);
        }

        public void Dispose()
        {
            if (LockTaken)
                Synchronizer.Lock.Exit(true);
        }
    }
}

public interface IThreadSynchronizer
{
    SynchronizationManager.SyncContext SynchronizeThread(TimeSpan span = default);
}