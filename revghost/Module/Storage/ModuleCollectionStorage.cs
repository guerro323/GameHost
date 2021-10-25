using System.Collections.Generic;
using revghost.IO.Storage;

namespace revghost.Module.Storage;

/// <summary>
/// Contains the modules that can be loaded
/// </summary>
public class ModuleCollectionStorage : IModuleCollectionStorage
{
    private IStorage _parent;

    public ModuleCollectionStorage(IStorage parent) => _parent = parent;

    public string CurrentPath => _parent.CurrentPath;

    public void GetFiles<TList>(string pattern, TList listToFill) where TList : IList<IFile>
    {
        _parent.GetFiles(pattern, listToFill);
    }

    public IStorage GetSubStorage(string path)
    {
        return _parent.GetSubStorage(path);
    }
}