using System;

namespace GameHost.V3.Injection.Dependencies
{
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
}