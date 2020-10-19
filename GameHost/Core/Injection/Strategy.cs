using System;

namespace GameHost.Injection
{
    public interface IStrategy
    {
    }

    public interface IDependencyStrategy : IStrategy
    {
        object       ResolveNow(Type  type);
        Func<object> GetResolver(Type type);
    }

    public struct ContextBindingStrategy : IStrategy
    {
        private Context ctx;
        private bool    resolveInParent;

        public ContextBindingStrategy(Context ctx, bool resolveInParent)
        {
            this.ctx             = ctx;
            this.resolveInParent = resolveInParent;
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            var result = ctx.Container.GetOrDefault(type);
            if (!resolveInParent || result != null)
                return result;

            var inner = ctx.Parent;
            while (result == null && inner != null)
            {
                result = inner.Container.GetOrDefault(type);
                inner  = inner.Parent;
            }

            return result;
        }
    }
}