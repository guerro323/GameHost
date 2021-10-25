using System;
using revghost.Injection.Dependencies;

namespace revghost.Injection;

public interface IDependencyCollection
{
    /// <summary>
    /// Available dependencies
    /// </summary>
    ReadOnlySpan<IDependency> Dependencies { get; }

    /// <summary>
    /// Add a dependency that will get resolved later
    /// </summary>
    /// <param name="dependency">Dependency</param>
    void Add(IDependency dependency);

    /// <summary>
    /// Try resolving the dependency right now
    /// </summary>
    /// <param name="canStillBeResolvedSynchronously">Whether or not it's possible to continue resolving without blocking</param>
    /// <returns>True if no dependencies are found</returns>
    bool TryResolve(out bool canStillBeResolvedSynchronously);
}