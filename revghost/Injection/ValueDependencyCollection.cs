using System;
using Collections.Pooled;
using revghost.Injection.Dependencies;

namespace revghost.Injection;

public struct ValueDependencyCollection<TContext> : IDependencyCollection
    where TContext : IReadOnlyContext
{
    private readonly PooledList<IDependency> _dependencies;
    private readonly TContext _context;

    public ValueDependencyCollection(TContext context, int capacity = 0)
    {
        _context = context;
        _dependencies = new PooledList<IDependency>(capacity);
    }

    public ReadOnlySpan<IDependency> Dependencies => _dependencies.Span;

    public void Add(IDependency dependency)
    {
        if (_dependencies is null)
            throw new InvalidOperationException($"{nameof(ValueDependencyCollection<TContext>)} was not setup");

        _dependencies.Add(dependency);
    }

    public bool TryResolve(out bool canStillBeResolvedSynchronously)
    {
        if (_dependencies is null)
            throw new InvalidOperationException($"{nameof(ValueDependencyCollection<TContext>)} was not setup");

        canStillBeResolvedSynchronously = _dependencies.Count > 0;

        var depCount = _dependencies.Count;
        while (depCount-- > 0)
        {
            if (_dependencies[0].IsResolved)
                continue;

            try
            {
                _dependencies[0].Resolve(_context);
            }
            catch (Exception ex)
            {
                _dependencies[0].ResolveException = ex;
            }

            if (!_dependencies[0].IsResolved)
                canStillBeResolvedSynchronously = false;
        }

        if (canStillBeResolvedSynchronously) _dependencies.Clear();

        return _dependencies.Count == 0;
    }
}