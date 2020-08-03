using System.Collections.Generic;
using DryIoc;

namespace GameHost.Injection
{
    public class Context
    {
        private HashSet<object> registeredObjects;

        private List<Context> childs;

        public IContainer Container { get; }
        public Context Parent { get; }

        public Context(Context parent, Rules rules = null)
        {
            Parent = parent;
            Parent?.childs?.Add(this);

            Container         = new Container(rules);
            registeredObjects = new HashSet<object>();
            childs            = new List<Context>();
        }

        public void Register(object obj)
        {
            registeredObjects.Add(obj);
            Container.UseInstance(obj);
        }

        public void Bind<TIn, TOut>() where TOut : TIn
        {
            Container.Register<TIn, TOut>();
        }

        public void BindExisting<TIn, TOut>(TOut o) where TOut : TIn
        {
            // bind self
            Register(o);
            Container.UseInstance<TIn>(o);
        }
        
        public void BindExisting<TInOut>(TInOut o)
        {
            BindExisting<TInOut, TInOut>(o);
        }
    }
}
