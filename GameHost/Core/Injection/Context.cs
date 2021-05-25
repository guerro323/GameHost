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
        private struct Registration
        {
            public bool AllowDisposal;
        }
        
        private Dictionary<object, Registration> registeredObjects;

        private List<Context> children;

        public IContainer Container { get; }
        public Context    Parent    { get; }

        public Context(Context parent)
        {
            Parent = parent;
            Parent?.children?.Add(this);

            Container         = new Container();
            registeredObjects = new();
            children          = new();
        }

        public void Register(object obj, Type type = null, bool allowDisposal = false)
        {
            if (type == null && obj == null)
                return;
            
            registeredObjects[obj] = new() {AllowDisposal = allowDisposal};
            Container.Use(obj, type);
        }

        public void Unregister(object obj)
        {
            registeredObjects.Remove(obj);
            Container.DeleteInstance(obj);

            if (new ContextBindingStrategy(this, false).Resolve(obj.GetType()) != null)
                throw new InvalidOperationException($"{obj.GetType()} should have been removed from this context");
        }

        public void BindExisting<TIn, TOut>(TOut o, bool allowDisposal = false) where TOut : TIn
        {
            // bind self
            Register(o, allowDisposal: allowDisposal);
            Container.Use(o, typeof(TIn));
        }

        public void BindExisting<TInOut>(TInOut o, bool allowDisposal = false)
        {
            BindExisting<TInOut, TInOut>(o, allowDisposal);
        }

        public void Dispose()
        {
            Parent?.children.Remove(this);
            
            Container?.Dispose();

            foreach (var (obj, registration) in registeredObjects)
            {
                if (!registration.AllowDisposal || obj is not IDisposable disposable)
                    continue;

                disposable.Dispose();
            }

            registeredObjects.Clear();
            
            foreach (var child in children.ToArray()) // make a copy since the child will remove itself in "children" list
                child.Dispose();

            children.Clear();
        }
    }
}