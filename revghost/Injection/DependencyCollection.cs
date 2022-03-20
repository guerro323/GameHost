using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Collections.Pooled;
using revghost.Injection.Dependencies;
using revghost.Utility;

namespace revghost.Injection;

public class DependencyCollection : IDependencyCollection, IHasDependencies, IDisposable
{
    public delegate void DependenciesCompleted(IReadOnlyList<object> dependencies);

    public delegate void DependenciesOnFinal();

    public readonly IReadOnlyContext Context;

    private readonly ConcurrentBag<TaskCompletionSource<bool>> dependencyCompletion = new();
    private readonly List<DependenciesCompleted> onCompleteList = new();
    private readonly List<DependenciesOnFinal> onFinalList = new();

    public readonly PooledList<IDependency> Queued = new();
    public readonly IDependencyResolver Resolver;

    public readonly string Source;

    private bool _isDisposed;

    public DependencyCollection(IReadOnlyContext context, IDependencyResolver resolver, string source = "")
    {
        Resolver = resolver;
        Context = context;
        Source = source;
    }

    public void Add(IDependency dependency)
    {
        Queued.Add(dependency);
        Resolver.Queue(this);
    }

    public ReadOnlySpan<IDependency> Dependencies => Queued.Span;

    public bool TryResolve(out bool canStillBeResolvedSynchronously)
    {
        if (_isDisposed)
        {
            canStillBeResolvedSynchronously = false;
            return false;
        }

        canStillBeResolvedSynchronously = Queued.Count > 0;

        var depCount = Queued.Count;
        for (var i = 0; i != Queued.Count; i++)
        {
            var dep = Queued[i];
            if (dep.IsResolved)
                continue;

            try
            {
                dep.Resolve(Context);
            }
            catch (Exception ex)
            {
                dep.ResolveException = ex;
                HostLogger.Output.Error(
                    $"Resolving on item '{dep}' for '{Source}' dependencies has failed!\n{ex}",
                    $"DependencyCollection({Source})",
                    "dependencies-failed-item"
                );
            }

            if (!dep.IsResolved)
                canStillBeResolvedSynchronously = false;
            else if (WaitingDependency.CanQueue(dep)) 
                Queued.Add(new WaitingDependency(dep));
        }

        if (canStillBeResolvedSynchronously && Queued.Count > 0)
        {
            using var resolvedDependencies = new PooledList<object>(Queued.Count);
            foreach (var dep in Queued)
                if (dep.IsResolved && dep is IResolvedObject resolvedObject)
                    resolvedDependencies.Add(resolvedObject.Resolved);

            lock (Queued)
            {
                Queued.Clear();

                // It's possible to add a new 'OnComplete' delegate when one is already being processed
                // which is why we only execute the delegates we had before and remove them after.
                var completedListCount = onCompleteList.Count;
                for (var i = 0; i < completedListCount; i++)
                {
                    onCompleteList[i](resolvedDependencies);
                }

                onCompleteList.RemoveRange(0, completedListCount);
            }
        }

        if (Queued.Count == 0)
        {
            if (dependencyCompletion.IsEmpty == false)
            {
                // Be sure to set the result right after onComplete has been called (in case new deps has been added)
                foreach (var tcs in dependencyCompletion)
                    tcs.SetResult(true);

                dependencyCompletion.Clear();
            }

            var finalListCount = onFinalList.Count;
            for (var i = 0; i < finalListCount; i++)
                onFinalList[i]();

            onFinalList.Clear();
        }

        if (Queued.Count != 0) 
            return true;
        
        canStillBeResolvedSynchronously = false;
        return false;

    }

    public void Dispose()
    {
        Resolver.Dequeue(this);

        Queued.Clear();
        onCompleteList.Clear();
        onFinalList.Clear();

        _isDisposed = true;
    }

    public Task ToTask()
    {
        lock (Queued)
        {
            if (Queued.Count == 0) return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();
        dependencyCompletion.Add(tcs);
        return tcs.Task;
    }

    public void OnComplete(DependenciesCompleted action)
    {
        onCompleteList.Add(action);
    }

    public void OnFinal(DependenciesOnFinal action)
    {
        onFinalList.Add(action);
        // OnFinal is the only other method to have Resolver.Queue()
        // The reason being that if there are no dependencies OnFinal would never be called if Queue wasn't called 
        Resolver.Queue(this);
    }

    IDependencyCollection IHasDependencies.Dependencies => this;
}