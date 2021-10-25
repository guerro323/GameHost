using System;
using System.Collections.Generic;
using revghost.Injection;
using revghost.Utility;

namespace revghost.Ecs;

/// <summary>
/// A system operate on data and can have dependencies.
/// </summary>
// Since AppSystem look much more like AppObject from GameHost.V2 then we could rename it?
public abstract class AppSystem : IDisposable, IHasDependencies
{
    // TODO: it is needed? If yes, then should OnDependenciesResolved and OnInit become an optional method?
    protected AppSystem(IDependencyCollection dependencyCollection) {}
        
    public AppSystem(Scope scope)
    {
        if (!scope.Context.TryGet(out IDependencyResolver resolver))
            throw new InvalidOperationException(
                $"Base constructor of {nameof(AppSystem)} is used, and no {nameof(IDependencyResolver)} is present in the given scope"
            );

        {
            var childScope = new ChildScopeContext(scope.Context);
            childScope.Register(typeof(object), this);
                
            var obj = new DependencyCollection(childScope, resolver, GetType().FullName);
            obj.OnComplete(OnDependenciesResolved);
            obj.OnFinal(OnInit);

            Dependencies = obj;

            // the scope need to be disposed or else the type will not cleared from the GC
            Disposables.Add(childScope);
            Disposables.Add(obj);
        }
    }

    public IList<IDisposable> Disposables { get; } = new List<IDisposable>();

    public void Dispose()
    {
        OnDispose();

        foreach (var d in Disposables)
            try
            {
                d.Dispose();
            }
            catch (Exception ex)
            {
                HostLogger.Output.Error(
                    $"Disposing '{d}' failed\n{ex}",
                    GetType().ToString(),
                    "system-disposing-failed"
                );
            }
    }

    public IDependencyCollection Dependencies { get; }

    protected virtual void OnDispose()
    {
    }

    protected virtual void OnDependenciesResolved(IReadOnlyList<object> dependencies) {}
    protected abstract void OnInit();
}