using System;

namespace revghost.Module;

[AttributeUsage(AttributeTargets.Assembly)]
public class EntryModuleAttribute : Attribute
{
    public Type TargetType;

    public EntryModuleAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}