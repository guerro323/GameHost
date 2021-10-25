using System;
using revghost.Injection.Dynamic;

namespace revghost.Injection.Dependencies;

public class ReturnByRefDependency<T> : IDependency, IResolvedObject
{
    public delegate ref T Delegate();

    private object _boxedResult;

    private T _unboxedResult;

    public Delegate Function;
    public Type Type;

    public ReturnByRefDependency(Type type, Delegate fun)
    {
        Type = type;
        Function = fun;
    }

    public Exception ResolveException { get; set; }

    public bool IsResolved { get; set; }

    public void Resolve<TContext>(TContext context)
        where TContext : IReadOnlyContext
    {
        if (context.TryGet(typeof(T), out _boxedResult))
        {
            switch (_boxedResult)
            {
                case DynamicDependency<T> dynamicDependencyT:
                    _unboxedResult = dynamicDependencyT.CreateT(context);
                    break;
                case DynamicDependency dynamicDependency:
                    _unboxedResult = (T) dynamicDependency.Create(context);
                    break;
                case IHasDependencies hasDependencies when !hasDependencies.Dependencies.Dependencies.IsEmpty:
                    return;
                default:
                    _unboxedResult = (T) _boxedResult;
                    break;
            }

            // The ref object will be replaced by the resolved.
            Function() = _unboxedResult;
            IsResolved = true;
            return;
        }

        IsResolved = false;
    }

    public object Resolved => _boxedResult ??= _unboxedResult;

    public override string ToString()
    {
        return $"Dependency(type={Type}, completed={IsResolved.ToString()})";
    }
}

public static class ReturnByRefDependencyExtensions
{
    public static void AddRef<T>(this IDependencyCollection dependencyCollection,
        ReturnByRefDependency<T>.Delegate func)
    {
        dependencyCollection.Add(new ReturnByRefDependency<T>(typeof(T), func));
    }
}