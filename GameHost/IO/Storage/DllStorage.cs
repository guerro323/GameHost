using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using GameHost.Core.IO;

namespace GameHost.IO
{
    public class DllStorage : IStorage
    {
        public Assembly Assembly { get; }

        /// <summary>
        /// Used for when we instantiate DllStorage as a child.
        /// </summary>
        private string parentPath;

        public DllStorage(Assembly assembly)
        {
            Assembly = assembly;
        }

        public string CurrentPath => new string(DllEmbeddedFile.ToDirectoryLike(Assembly.GetName().Name)) + (parentPath ?? string.Empty);

        public Task<IEnumerable<IFile>> GetFilesAsync(string pattern)
        {
            return Task.FromResult(Assembly.GetManifestResourceNames()
                                           .Select(mrn => (IFile)new DllEmbeddedFile(Assembly, mrn))
                                           .Where(file => FileSystemName.MatchesSimpleExpression(pattern, file.FullName.Replace(CurrentPath + "/", string.Empty))));
        }

        public Task<IStorage> GetOrCreateDirectoryAsync(string path)
        {
            var success = Assembly.GetManifestResourceNames().Any(name => DllEmbeddedFile.ToDirectoryLike(name).StartsWith(CurrentPath + "/" + path));
            if (!success)
                throw new InvalidOperationException("No such directory in path: " + path);

            return Task.FromResult((IStorage)new ChildStorage(this, new DllStorage(Assembly) {parentPath = "/" + path}));
        }

        public override string ToString()
        {
            return $"DllStorage(Path={CurrentPath})";
        }
    }

    public class DllEmbeddedFile : IFile
    {
        public Assembly Assembly { get; }

        public string ManifestName { get; }
        public string Name { get; }
        public string FullName { get; }

        public static ReadOnlySpan<char> ToDirectoryLike(string origin, bool ignoreExtension = true)
        {
            var lastDotIndex = ignoreExtension ? origin.Length : origin.LastIndexOf('.');
            var chars        = new Span<char>(origin.ToCharArray());
            for (var i = 0; i < lastDotIndex; i++)
            {
                if (chars[i] == '.')
                    chars[i] = '/';
            }

            return chars;
        }

        public DllEmbeddedFile(Assembly assembly, string manifestName)
        {
            Assembly     = assembly;
            ManifestName = manifestName;
            FullName     = new string(ToDirectoryLike(ManifestName, ignoreExtension: false));
            Name         = Path.GetFileName(FullName);
        }

        public async Task<byte[]> GetContentAsync()
        {
            await using var stream = Assembly.GetManifestResourceStream(ManifestName);
            // the -3 +3 is kept for historical reason in comments, since opening .xaml files in visual studio will automatically add a BOM in the beginning of the file...
            // so each time this will happen, seeing this comment will save me hours of pain
            var mem = new byte[stream.Length /*- 3*/];
            //stream.Position += 3;
            await stream.ReadAsync(mem, 0, mem.Length);
            return mem;
        }
    }
}
