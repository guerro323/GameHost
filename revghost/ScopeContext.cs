using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using revghost.Injection;

namespace revghost;

public static class ScopeContextExtensions
{
    public static void Register<T>(this ScopeContext ctx, T obj)
    {
        ctx.Register(typeof(T), obj);
        ctx.Register(obj.GetType(), obj);
    }

    public static void Register(this ScopeContext ctx, object obj)
    {
        ctx.Register(obj.GetType(), obj);
    }
}

public class ScopeContext : IReadOnlyContext, IDisposable
{
    private readonly Dictionary<Type, object> _objectMap = new();

    public virtual void Dispose()
    {
        _objectMap.Clear();
        Disposed?.Invoke();
    }

    public virtual bool TryGet(Type type, out object obj)
    {
        return _objectMap.TryGetValue(type, out obj);
    }

    public event Action Disposed;

    public void Register(Type type, object obj)
    {
        _objectMap[type] = obj;
    }
}

public class ChildScopeContext : ScopeContext
{
    public ScopeContext Parent;

    public ChildScopeContext(ScopeContext parent)
    {
        Parent = parent;
        Parent.Disposed += Dispose;
    }

    public override void Dispose()
    {
        if (Parent != null)
        {
            Parent.Disposed -= Dispose;
            Parent = null;
        }

        base.Dispose();
    }

    public override bool TryGet(Type type, out object obj)
    {
        if (Parent == null)
            throw new InvalidOperationException("This context was disposed");
        
        return base.TryGet(type, out obj) || Parent.TryGet(type, out obj);
    }
}

public class MultipleScopeContext : ScopeContext, IEnumerable<ScopeContext>
{
    public ScopeContext[] Contexts { get; private set; }

    public MultipleScopeContext()
    {
        Contexts = Array.Empty<ScopeContext>();
    }
        
    public MultipleScopeContext(IEnumerable<ScopeContext> contexts)
    {
        Contexts = contexts.ToArray();
    }

    public void Add(ScopeContext context)
    {
        var ctx = Contexts;
        Array.Resize(ref ctx, Contexts.Length + 1);
        Contexts = ctx;

        Contexts[^1] = context;
    }

    public override bool TryGet(Type type, out object obj)
    {
        if (base.TryGet(type, out obj))
            return true;

        // Get from the latest context first

        var count = Contexts.Length;
        while (count-- > 0)
        {
            if (Contexts[count].TryGet(type, out obj))
                return true;
        }

        return false;
    }

    public IEnumerator<ScopeContext> GetEnumerator()
    {
        return MemoryMarshal.ToEnumerable<ScopeContext>(Contexts).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}