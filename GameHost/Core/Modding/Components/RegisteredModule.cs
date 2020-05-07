namespace GameHost.Core.Modding.Components
{
    public struct RegisteredModule
    {
        public SModuleInfo Info;
        public ModuleState State;
    }

    public enum ModuleState
    {
        None,
        IsLoading,
        Loaded,
        Unloading
    }
}
