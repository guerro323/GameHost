using System.Collections.Generic;
using GameHost.V3.IO.Storage;

namespace GameHost.V3.Domains
{
    public class ExecutiveStorage : IStorage
    {
        private readonly IStorage parent;

        public string CurrentPath => parent.CurrentPath;

        public ExecutiveStorage(IStorage parent)
        {
            this.parent = parent;
        }

        public void GetFiles<TList>(string pattern, TList listToFill) where TList : IList<IFile>
        {
            parent.GetFiles(pattern, listToFill);
        }

        public IStorage GetSubStorage(string path)
        {
            return parent.GetSubStorage(path);
        }
    }
}