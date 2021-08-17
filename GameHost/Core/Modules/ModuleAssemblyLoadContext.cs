using System.Reflection;
using System.Runtime.Loader;

namespace GameHost.Core.Modules
{
	public class ModuleAssemblyLoadContext : AssemblyLoadContext
	{
		public ModuleAssemblyLoadContext()
#if !NETSTANDARD
			: base(nameof(ModuleAssemblyLoadContext), isCollectible: true)
#endif
		{
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			return null;
		}
	}

	public class NamedAssemblyLoadContext : AssemblyLoadContext
	{
		public NamedAssemblyLoadContext(string name)
#if !NETSTANDARD
			: base(name)
#endif
		{
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			return null;
		}
	}
}