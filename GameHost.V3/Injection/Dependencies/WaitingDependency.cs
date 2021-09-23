using System;

namespace GameHost.V3.Injection.Dependencies
{
    public class WaitingDependency : IDependency, IResolvedObject
    {
        public WaitingDependency(IDependency obj) => Resolved = ((IResolvedObject) obj).Resolved;

        public WaitingDependency(object obj) => Resolved = obj;

        public Exception ResolveException { get; set; }
        public bool IsResolved { get; private set; }

        public void Resolve<TContext>(TContext context) where TContext : IReadOnlyContext
        {
            var hasDependencies = (IHasDependencies) Resolved;
            IsResolved = hasDependencies.Dependencies.Dependencies.IsEmpty;
        }

        public object Resolved { get; }

        public static bool CanQueue(IDependency dep)
        {
            if (dep is WaitingDependency || dep.IsResolved == false)
                return false;

            if (dep is IResolvedObject {Resolved: not IHasDependencies}) 
                return false;
            
            return true;
        }
    }
}