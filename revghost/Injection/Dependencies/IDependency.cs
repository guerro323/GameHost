using System;

namespace revghost.Injection.Dependencies;

public interface IResolvedObject
{
    object Resolved { get; }
}

public interface IDependency
{
    public Exception ResolveException { get; set; }

    public bool IsResolved { get; }

    void Resolve<TContext>(TContext context) where TContext : IReadOnlyContext;
}