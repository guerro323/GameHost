using System.Reflection;
using System.Runtime.Loader;

namespace GameHost.Core.Modules
{
	public class ModuleAssemblyLoadContext : AssemblyLoadContext
	{
		public ModuleAssemblyLoadContext() : base(nameof(ModuleAssemblyLoadContext), isCollectible: true)
		{
		}

		protected override Assembly Load(AssemblyName assemblyName)
		{
			return null;
		}
	}
}