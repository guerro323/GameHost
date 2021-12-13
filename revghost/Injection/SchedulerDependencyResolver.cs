using System;
using System.Collections.Generic;
using Collections.Pooled;
using revghost.Shared.Threading.Schedulers;
using revghost.Threading;

namespace revghost.Injection;

public class SchedulerDependencyResolver : IDependencyResolver, IDisposable
{
    private static readonly Func<SchedulerDependencyResolver, bool> updateMethod = resolver =>
    {
        // Early stop if we've disposed
        if (resolver._isDisposed)
            return true;

        var isFinished = true;
        for (var i = 0; i < resolver._collections.Count; i++)
        {
            var collection = resolver._collections[i];
            bool wantToContinue;
            do
            {
                collection.TryResolve(out wantToContinue);
            } while (wantToContinue);

            if (collection.Dependencies.IsEmpty)
                // swapback
                resolver._collections.RemoveAt(i--);
            else
                isFinished = false;
        }

        return isFinished;
    };

    private static readonly Func<(SchedulerDependencyResolver, IDependencyCollection), bool> addCollection = tuple =>
    {
        var (resolver, collection) = tuple;
        if (resolver._isDisposed)
            return true;
        
        resolver._collections.Add(collection);
        
        resolver._scheduler.Add(updateMethod, resolver, SchedulingParametersWithArgs.AsOnceWithArgs);
        return true;
    };
    
    private static readonly Func<(SchedulerDependencyResolver, IDependencyCollection), bool> removeCollection = tuple =>
    {
        var (resolver, collection) = tuple;
        if (resolver._isDisposed)
            return true;
        
        resolver._collections.Remove(collection);
        return true;
    };

    private readonly PooledList<IDependencyCollection> _collections = new();
    private readonly IScheduler _scheduler;

    private bool _isDisposed;

    public SchedulerDependencyResolver(IScheduler scheduler)
    {
        _scheduler = scheduler;
    }

    public void Queue(IDependencyCollection collection)
    {
        _scheduler.Add(addCollection, (this, collection), SchedulingParametersWithArgs.AsOnceWithArgs);
    }

    public void Dequeue(IDependencyCollection collection)
    {
        _scheduler.Add(removeCollection, (this, collection), SchedulingParametersWithArgs.AsOnceWithArgs);
    }

    public void Dispose()
    {
        _collections?.Dispose();

        _isDisposed = true;
    }

    public void GetQueuedCollections<TList>(ref TList list)
        where TList : IList<IDependencyCollection>
    {
        foreach (var c in _collections)
            list.Add(c);
    }
}