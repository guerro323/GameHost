using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameHost.Core.IO;

namespace GameHost.IO
{
    public class DllStorage : IStorage
    {
        public Assembly Assembly { get; }

        public DllStorage(Assembly assembly)
        {
            Assembly = assembly;
        }

        public string CurrentPath => Assembly.GetName().Name;
        
        public Task<IEnumerable<IFile>> GetFilesAsync(string pattern)
        {
            return Task.FromResult(Assembly.GetManifestResourceNames().Select(mrn => (IFile) new DllEmbeddedFile(Assembly, mrn)));
        }

        public Task<IStorage> GetOrCreateDirectoryAsync(string path)
        {
            throw new NullReferenceException($"Can not invoke '{nameof(GetOrCreateDirectoryAsync)}' in a '{nameof(DllStorage)}'");
        }
    }

    public class DllEmbeddedFile : IFile
    {
        public Assembly Assembly { get; }

        public string ManifestName { get; }
        public string Name { get; }
        public string FullName { get; }

        public DllEmbeddedFile(Assembly assembly, string manifestName)
        {
            Assembly     = assembly;
            ManifestName = manifestName;
            var lastDotIndex = ManifestName.LastIndexOf('.');
            var chars = new Span<char>(ManifestName.ToCharArray());
            for (var i = 0; i < lastDotIndex; i++)
            {
                if (chars[i] == '.')
                    chars[i] = '/';
            }
            FullName = new string(chars);
            Name         = Path.GetFileName(FullName);
        }

        public async Task<byte[]> GetContentAsync()
        {
            await using var stream = Assembly.GetManifestResourceStream(ManifestName);

            var mem = new byte[stream.Length - 3];
            stream.Position += 3;
            await stream.ReadAsync(mem, 0, mem.Length);
            return mem;
        }
    }
}
