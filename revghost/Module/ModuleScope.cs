using revghost.IO.Storage;

namespace revghost.Module;

public class ModuleScope : Scope
{
    public readonly IStorage DataStorage;
    public readonly DllStorage DllStorage;

    public ModuleScope(HostModule module, Scope parentScope) : base(new ChildScopeContext(parentScope.Context))
    {
        DataStorage = module.CreateDataStorage(parentScope);
        DllStorage = new DllStorage(module.GetType().Assembly);

        Context.Register(DataStorage);
        Context.Register(DllStorage);

        Context.Register(module.GetType().Assembly);
    }
}