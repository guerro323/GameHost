#if NETSTANDARD2_1 || NETCOREAPP
#define ADVANCED_ALC
#endif

using System.Reflection;
using System.Runtime.Loader;

namespace revghost.Module;

public class ModuleAssemblyLoadContext : AssemblyLoadContext
{
    public ModuleAssemblyLoadContext()
#if ADVANCED_ALC
        : base(nameof(ModuleAssemblyLoadContext), isCollectible: true)
#endif
    {
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
#if ADVANCED_ALC
        return base.Load(assemblyName);
#else
            return null;
#endif
    }
}

public class NamedAssemblyLoadContext : AssemblyLoadContext
{
    public NamedAssemblyLoadContext(string name)
#if ADVANCED_ALC
        : base(name)
#endif
    {
    }

    protected override Assembly Load(AssemblyName assemblyName)
    {
        return null;
    }
}