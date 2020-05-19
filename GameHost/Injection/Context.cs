using System.Collections.Generic;
using DryIoc;
using GameHost.Core.Applications;

namespace GameHost.Injection
{
    public class Context
    {
        private HashSet<object> registeredObjects;
        private SignalerMap     signaler;
        
        private List<Context> childs;

        public IContainer Container { get; }
        public Context Parent { get; }

        public Context(Context parent, Rules rules = null)
        {
            Parent = parent;
            Parent?.childs?.Add(this);

            Container         = new Container(rules);
            registeredObjects = new HashSet<object>();
            signaler          = new SignalerMap();
            childs            = new List<Context>();
        }

        public void Register(object obj)
        {
            if (registeredObjects.Add(obj))
                signaler.Version++;
            
            Container.UseInstance(obj);
        }

        public void Bind<TIn, TOut>() where TOut : TIn
        {
            Container.Register<TIn, TOut>();
        }

        public void Bind<TIn, TOut>(TOut o) where TOut : TIn
        {
            // bind self
            Register(o);
            Container.UseInstance<TIn>(o);
        }
        
        public void Bind<TInOut>(TInOut o)
        {
            Bind<TInOut, TInOut>(o);
        }

        public void SignalApp<T>(in T data, bool recurse = true, bool childFirst = false)
            where T : IAppEvent
        {
            if (recurse && childFirst)
                foreach (var c in childs)
                    c.SignalApp(data, recurse, childFirst);

            signaler.SignalApp(data, registeredObjects);

            if (recurse && !childFirst)
                foreach (var c in childs)
                    c.SignalApp(data, recurse, childFirst);
        }

        public void SignalData<T>(ref T data, bool recurse = true, bool childFirst = false)
            where T : IDataEvent
        {
            if (recurse && childFirst)
                foreach (var c in childs)
                    c.SignalData(ref data, recurse, childFirst);

            signaler.SignalData(ref data, registeredObjects);

            if (recurse && !childFirst)
                foreach (var c in childs)
                    c.SignalData(ref data, recurse, childFirst);
        }
    }
}
