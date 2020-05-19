using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.IO;

namespace GameHost.IO
{
    public class LocalStorage : IStorage
    {
        private DirectoryInfo directory;

        public LocalStorage(DirectoryInfo directory, bool create = true)
        {
            if (!directory.Exists)
                directory.Create();

            this.directory = directory;
        }

        public LocalStorage(string directory, bool create = true) : this(new DirectoryInfo(directory), create) { }

        public string CurrentPath => directory.FullName;

        public Task<IEnumerable<IFile>> GetFilesAsync(string pattern)
        {
            return Task.FromResult(directory.GetFiles(pattern).Select(f => (IFile) new LocalFile(f)));
        }

        public Task<IStorage> GetOrCreateDirectoryAsync(string path)
        {
            var target = directory.CreateSubdirectory(path);
            return Task.FromResult((IStorage)new LocalStorage(target));
        }
    }

    public class LocalFile : IFile
    {
        private readonly FileInfo file;

        public string Name     => file.Name;
        public string FullName => file.FullName;

        public async Task<byte[]> GetContentAsync()
        {
            await using var stream = File.Open(FullName, FileMode.Open);

            var mem = new byte[stream.Length];
            await stream.ReadAsync(mem, 0, mem.Length);
            return mem;
        }

        public LocalFile(FileInfo file)
        {
            this.file = file;
        }
    }
}
