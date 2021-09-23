using System;

namespace GameHost.V3
{
    public class Scope : IDisposable
    {
        public readonly ScopeContext Context;

        protected Scope(ScopeContext customContext, bool registerSelf = true)
        {
            Context = customContext;

            if (registerSelf)
                Context.Register(this);
        }

        public Scope() : this(new ScopeContext())
        {
        }

        public virtual void Dispose()
        {
            Context.Dispose();
        }
    }

    public class FreeScope : Scope
    {
        public FreeScope(ScopeContext context) : base(context) {}
    }
}