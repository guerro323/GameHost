using System;
using System.Reflection;
using revghost.Injection.Dynamic;

namespace revghost.Injection.Dependencies;

public class Dependency : IDependency, IResolvedObject
{
    private object _resolved;

    public Type Type;

    public Dependency(Type type)
    {
        Type = type;
    }

    public Exception ResolveException { get; set; }
    public bool IsResolved { get; set; }

    public void Resolve<TContext>(TContext context) where TContext : IReadOnlyContext
    {
        IsResolved = context.TryGet(Type, out _resolved);
        if (_resolved is DynamicDependency dynamicDependency)
        {
            _resolved = dynamicDependency.Create(context);
        }
    }

    public object Resolved => _resolved;

    public override string ToString()
    {
        return $"Dependency(type={Type}, completed={IsResolved.ToString()})";
    }
}

public class ReflectionDependency : IDependency, IResolvedObject
{
    public readonly MemberInfo MemberInfo;

    public readonly object This;

    public readonly Dependency Base;

    public ReflectionDependency(MemberInfo memberInfo, object @this)
    {
        MemberInfo = memberInfo;

        This = @this;

        Base = new Dependency(memberInfo switch
        {
            PropertyInfo i => i.PropertyType,
            FieldInfo i => i.FieldType,
            _ => throw new InvalidOperationException(nameof(memberInfo.GetType))
        });
    }

    public Exception ResolveException { get; set; }
    public bool IsResolved => Base.IsResolved;

    public void Resolve<TContext>(TContext context) where TContext : IReadOnlyContext
    {
        Base.Resolve(context);
        if (IsResolved)
        {
            switch (MemberInfo)
            {
                case PropertyInfo property:
                {
                    if (property.SetMethod != null)
                        property.SetValue(This, Resolved);
                    else
                    {
                        var field = This.GetType().GetField(
                            $"<{property.Name}>k__BackingField",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        );
                        field!.SetValue(This, Resolved);
                    }

                    break;
                }

                case FieldInfo field:
                {
                    field.SetValue(This, Resolved);
                    break;
                }
            }
        }
    }

    public object Resolved => Base.Resolved;
}