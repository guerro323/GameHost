using System;
using GameHost.V3.Injection.Dependencies;

namespace GameHost.V3.Injection
{
    public interface IDependencyCollection
    {
        ReadOnlySpan<IDependency> Dependencies { get; }

        void Add(IDependency dependency);

        bool TryResolve(out bool canStillBeResolvedSynchronously);
    }
}