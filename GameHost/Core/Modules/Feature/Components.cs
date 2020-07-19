using DefaultEcs;

namespace GameHost.Core.Modules.Feature
{
	public struct RefreshModuleList
	{
	}

	public struct RegisteredModule
	{
		public GameHostModuleDescription Description;
		public ModuleState               State;
	}

	public enum ModuleState
	{
		None,
		IsLoading,
		Loaded,
		Unloading
	}
	
	public struct RequestLoadModule
	{
		public Entity Module;
	}

	public struct RequestUnloadModule
	{
		public Entity Module;
	}
}