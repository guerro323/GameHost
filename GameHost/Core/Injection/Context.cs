using System;
using System.Collections.Generic;

namespace GameHost.Injection
{
    public interface IContainer : IDisposable
    {
        bool TryGet(Type           type, out object obj);
        void Use(object            obj,  Type       target = null);
        void DeleteType(Type       type);
        void DeleteInstance(object obj);

        T GetOrDefault<T>()
        {
            return TryGet(typeof(T), out var obj) ? (T) obj : default;
        }
        
        object GetOrDefault(Type type)
        {
            return TryGet(type, out var obj) ? obj : default;
        }
    }

    public class Container : IContainer
    {
        private Dictionary<Type, object> objectMap = new Dictionary<Type, object>();

        public void Dispose()
        {
            objectMap.Clear();
        }

        public bool TryGet(Type type, out object obj)
        {
            return objectMap.TryGetValue(type, out obj);
        }

        public void Use(object obj, Type target = null)
        {
            objectMap[target ?? obj?.GetType() ?? throw new InvalidOperationException("Null Type and Null Object!")] = obj;
        }

        public void DeleteType(Type type)
        {
            objectMap.Remove(type);
        }

        public void DeleteInstance(object obj)
        {
            if (!objectMap.ContainsValue(obj))
                return;

            var keys = new List<Type>();
            foreach (var (type, o) in objectMap)
                if (ReferenceEquals(obj, o) || Equals(obj, o))
                    keys.Add(type);
            
            while (keys.Count > 0)
            {
                objectMap.Remove(keys[0]);
                keys.RemoveAt(0);
            }
        }
    }

    public class Context : IDisposable
    {
        private HashSet<object> registeredObjects;

        private List<Context> childs;

        public IContainer Container { get; }
        public Context    Parent    { get; }

        public Context(Context parent)
        {
            Parent = parent;
            Parent?.childs?.Add(this);

            Container         = new Container();
            registeredObjects = new HashSet<object>();
            childs            = new List<Context>();
        }

        public void Register(object obj, Type type = null)
        {
            if (type == null && obj == null)
                return;
            
            registeredObjects.Add(obj);
            Container.Use(obj, type);
        }

        public void Unregister(object obj)
        {
            registeredObjects.Remove(obj);
            Container.DeleteInstance(obj);

            if (new ContextBindingStrategy(this, false).Resolve(obj.GetType()) != null)
                throw new InvalidOperationException($"{obj.GetType()} should have been removed from this context");
        }

        public void BindExisting<TIn, TOut>(TOut o) where TOut : TIn
        {
            // bind self
            Register(o);
            Container.Use(o, typeof(TIn));
        }

        public void BindExisting<TInOut>(TInOut o)
        {
            BindExisting<TInOut, TInOut>(o);
        }

        public void Dispose()
        {
            Parent?.childs.Remove(this);
            
            Container?.Dispose();
            registeredObjects.Clear();
            foreach (var child in childs)
                child.Dispose();

            childs.Clear();
        }
    }
}