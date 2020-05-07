using System;
using JetBrains.Annotations;

namespace GameHost.Injection
{
    [MeansImplicitUseAttribute]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DependencyStrategyAttribute : Attribute
    {

    }
}
