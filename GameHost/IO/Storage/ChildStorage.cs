using System.Collections.Generic;
using System.Threading.Tasks;
using GameHost.Core.IO;

namespace GameHost.IO
{
    public class ChildStorage : IStorage
    {
        public readonly IStorage root;
        public readonly IStorage parent;
        
        public string CurrentPath => parent.CurrentPath;

        public ChildStorage(IStorage root, IStorage parent)
        {
            this.root   = root ?? parent;
            this.parent = parent ?? root;
        }

        public Task<IEnumerable<IFile>> GetFilesAsync(string pattern)
        {
            return parent.GetFilesAsync(pattern);
        }

        public async Task<IStorage> GetOrCreateDirectoryAsync(string path)
        {
            return new ChildStorage(root, await parent.GetOrCreateDirectoryAsync(path));
        }

        public override string ToString()
        {
            return $"ChildStorage({parent})";
        }
    }
}
