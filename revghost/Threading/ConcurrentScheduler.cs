using System;
using System.Collections.Generic;
using System.Runtime.Loader;
using System.Threading;
using revghost.Shared.Threading.Schedulers;
using revghost.Utility;

namespace revghost.Threading;

public class ConcurrentScheduler : IRunnableScheduler
{
    private readonly TimeSpan _timeout;

    /// <summary>
    /// If true, continue the scheduler Run() on exception.
    /// </summary>
    public Func<Exception, bool> OnExceptionFound;

    private Queue<ScheduledValueTask> scheduledValueTasks;
    private List<ScheduledValueTask> nextRunningTasks;

    private Dictionary<Type, Collection> scheduledCollectionTypeMap;
    private Dictionary<Type, Collection> runningCollectionTypeMap;

    private Delegate debugLastTask;

    private SpinLock spinLock;

    private List<Action> contextUnloadMethods = new();

    public ConcurrentScheduler(Func<Exception, bool> onExceptionFound = null, TimeSpan defaultLockTimeout = default)
    {
        _timeout = defaultLockTimeout == TimeSpan.Zero ? TimeSpan.FromSeconds(1) : defaultLockTimeout;

        this.OnExceptionFound = onExceptionFound ?? (ex =>
        {
            HostLogger.Output.Error(ex, "ConcurrentScheduler", "exception-found");
            return false;
        });

        scheduledValueTasks = new Queue<ScheduledValueTask>();
        nextRunningTasks = new List<ScheduledValueTask>();

        scheduledCollectionTypeMap = new Dictionary<Type, Collection>();
        runningCollectionTypeMap = new Dictionary<Type, Collection>();

        spinLock = new SpinLock(true);
    }

    public void Dispose()
    {
        debugLastTask = null;
        foreach (var method in contextUnloadMethods)
            method();

        foreach (var (_, value) in scheduledCollectionTypeMap)
            value.Clear();
        foreach (var (_, value) in runningCollectionTypeMap)
            value.Clear();

        scheduledCollectionTypeMap.Clear();
        runningCollectionTypeMap.Clear();

        scheduledValueTasks.Clear();
        nextRunningTasks.Clear();

        OnExceptionFound = null;
    }

    public void Add<T>(Func<T, bool> action, T args, in SchedulingParametersWithArgs parameters)
    {
        var lockTaken = false;
        spinLock.TryEnter(_timeout, ref lockTaken);
        if (lockTaken)
        {
            if (!scheduledCollectionTypeMap.TryGetValue(typeof(T), out var collection))
            {
                collection = new Collection<T>();

                scheduledCollectionTypeMap[typeof(T)] = collection;
                runningCollectionTypeMap[typeof(T)] = new Collection<T>();

                void remove(AssemblyLoadContext ctx)
                {
                    ctx.Unloading -= remove;

                    scheduledCollectionTypeMap.Remove(typeof(T));
                    runningCollectionTypeMap.Remove(typeof(T));
                }

                // Make sure that when the assembly of that type is getting unloaded we don't keep a reference to it.
                AssemblyLoadContext.GetLoadContext(typeof(T).Assembly)!
                    .Unloading += remove;

                contextUnloadMethods.Add(() =>
                {
                    AssemblyLoadContext.GetLoadContext(typeof(T).Assembly)!
                        .Unloading -= remove;
                });
            }

            if (parameters.OnceWithMethod && collection.Contains(action))
            {
                spinLock.Exit(true);
                return;
            }

            if (parameters.OnceWithMethodAndArgs)
            {
                foreach (var t in ((Collection<T>) collection).Scheduled)
                {
                    if (t.action == action && t.args.Equals(args))
                    {
                        spinLock.Exit(true);
                        return;
                    }
                }
            }

            scheduledValueTasks.Enqueue(((Collection<T>) collection).Enqueue(action, args));
            spinLock.Exit(true);
        }
        else
        {
            HostLogger.Output.Error(
                $"Couldn't enter scheduler (action={action.Method}, prev={debugLastTask.Method})",
                "ConcurrentScheduler",
                "lock-error"
            );
        }
    }

    public void Run()
    {
        var lockTaken = false;
        spinLock.TryEnter(_timeout, ref lockTaken);
        if (lockTaken)
        {
            try
            {
                foreach (var (type, scheduled) in scheduledCollectionTypeMap)
                {
                    var running = runningCollectionTypeMap[type];
                    running.TakeFrom(scheduled);
                }

                nextRunningTasks.Clear();
                while (scheduledValueTasks.TryDequeue(out var task))
                    nextRunningTasks.Add(task);
            }
            finally
            {
                spinLock.Exit(true);
            }
        }
        else
            throw new InvalidOperationException("Couldn't Sync!");

        foreach (var task in nextRunningTasks)
        {
            try
            {
                debugLastTask = task.Invoke(
                    runningCollectionTypeMap,
                    scheduledCollectionTypeMap,
                    scheduledValueTasks
                );
            }
            catch (Exception ex)
            {
                if (!OnExceptionFound(ex))
                    return;
            }
        }
    }

    private abstract class Collection
    {
        public abstract Delegate DequeueAndInvoke(IReadOnlyDictionary<Type, Collection> schedulingMap, Queue<ScheduledValueTask> tasks);
        public abstract bool Contains(Delegate del);
        public abstract void TakeFrom(Collection other);

        public abstract void Clear();
    }

    private struct ScheduledValueTask
    {
        public Type Type;

        public Delegate Invoke(IReadOnlyDictionary<Type, Collection> runningMap,
            IReadOnlyDictionary<Type, Collection> schedulingMap,
            Queue<ScheduledValueTask> tasks)
        {
            return runningMap[Type].DequeueAndInvoke(schedulingMap, tasks);
        }
    }

    private class Collection<T> : Collection
    {
        public Queue<(Func<T, bool> action, T args)> Scheduled = new(4);

        public ScheduledValueTask Enqueue(Func<T, bool> action, in T args)
        {
            Scheduled.Enqueue((action, args));
            return new ScheduledValueTask {Type = typeof(T)};
        }

        public override Delegate DequeueAndInvoke(IReadOnlyDictionary<Type, Collection> schedulingMap, Queue<ScheduledValueTask> tasks)
        {
            if (!Scheduled.TryDequeue(out var pool))
                return null;

            if (!pool.action(pool.args))
            {
                (schedulingMap[typeof(T)] as Collection<T>)!.Enqueue(pool.action, pool.args);
                tasks.Enqueue(new ScheduledValueTask {Type = typeof(T)});
            }

            return pool.action;
        }

        public override bool Contains(Delegate del)
        {
            foreach (var scheduled in Scheduled)
                if (scheduled.action == (Func<T, bool>) del)
                    return true;
            return false;
        }

        public override void TakeFrom(Collection other)
        {
            var same = (Collection<T>) other;
            while (same.Scheduled.TryDequeue(out var action))
                Scheduled.Enqueue(action);
        }

        public override void Clear()
        {
            Scheduled.Clear();
        }
    }
}