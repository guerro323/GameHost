using System;

namespace GameHost.V3.Injection
{
    /// <summary>
    /// Encapsulate an existing context to have dependency support
    /// </summary>
    public class DependencyContext : IReadOnlyContext
    {
        public readonly ScopeContext Parent;

        public DependencyContext(ScopeContext parent)
        {
            Parent = parent;
        }
        
        /// <returns>Return the result of the parent context but return false if it has dependencies</returns>
        public bool TryGet(Type type, out object obj)
        {
            if (Parent.TryGet(type, out obj))
            {
                return obj is not IHasDependencies hasDependencies || hasDependencies.Dependencies.Dependencies.IsEmpty;
            }

            return false;
        }
    }
}