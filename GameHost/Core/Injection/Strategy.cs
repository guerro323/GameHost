using System;
using DryIoc;

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
        private bool resolveInParent;

        public ContextBindingStrategy(Context ctx, bool resolveInParent)
        {
            this.ctx = ctx;
            this.resolveInParent = resolveInParent;
        }

        public T Resolve<T>()
        {
            return (T) Resolve(typeof(T));
        }

        public object Resolve(Type type)
        {
            var result = ctx.Container.Resolve(type, resolveInParent ? IfUnresolved.ReturnDefault : IfUnresolved.Throw);
            if (!resolveInParent || result != null)
                return result;

            var inner = ctx.Parent;
            while (result == null && inner != null)
            {
                result = inner.Container.Resolve(type, IfUnresolved.ReturnDefault);
                inner  = inner.Parent;
            }

            return result;
        }
    }
    
    public struct InjectPropertyStrategy : IStrategy
    {
        private Context ctx;
        private bool    resolveInParent;

        public InjectPropertyStrategy(Context ctx, bool resolveInParent)
        {
            this.ctx             = ctx;
            this.resolveInParent = resolveInParent;
        }

        public void Inject(object instance)
        {
            if (resolveInParent)
            {
                var inner = ctx.Parent;
                while (inner != null)
                {
                    inner.Container.InjectPropertiesAndFields(instance);
                    inner  = inner.Parent;
                }
            }

            ctx.Container.InjectPropertiesAndFields(instance);
        }
    }
}
