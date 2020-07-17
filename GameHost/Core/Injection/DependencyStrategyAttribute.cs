using System;
using JetBrains.Annotations;

namespace GameHost.Injection
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DependencyStrategyAttribute : Attribute
    {

    }
}
