using System.Collections.Generic;

namespace GameHost.V3.IO.Storage
{
    public class ChildStorage : IStorage
    {
        public readonly IStorage Parent;
        public readonly IStorage Root;

        public ChildStorage(IStorage root, IStorage parent)
        {
            Root = root ?? parent;
            Parent = parent ?? root;
        }

        public string CurrentPath => Parent.CurrentPath;

        public void GetFiles<TList>(string pattern, TList listToFill) where TList : IList<IFile>
        {
            Parent.GetFiles(pattern, listToFill);
        }

        public IStorage GetSubStorage(string path)
        {
            return new ChildStorage(Root, Parent.GetSubStorage(path));
        }
    }
}