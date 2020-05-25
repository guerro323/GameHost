using System.IO;
using System.Threading.Tasks;
using GameHost.Core.IO;
using ICSharpCode.SharpZipLib.Zip;

namespace GameHost.IO
{
    /// <summary>
    /// Represent an entry from a zip file
    /// </summary>
    /// <remarks>
    /// The zip is only opened when <see cref="GetContentAsync"/> is called (and then closed when that call has been completed)
    /// </remarks>
    public class ZipEntryFile : IFile
    {
        private readonly string zipPath;

        public string Name     { get; }
        public string FullName { get; }

        public async Task<byte[]> GetContentAsync()
        {
            using var archive = new ZipFile(zipPath);
            var       entry   = archive.GetEntry(FullName);

            await using var stream = archive.GetInputStream(entry);

            var mem = new byte[entry.Size];
            await stream.ReadAsync(mem, 0, mem.Length);

            return mem;
        }

        public ZipEntryFile(string zipPath, ZipEntry entry)
        {
            this.zipPath = zipPath;

            Name     = Path.GetFileName(entry.Name);
            FullName = entry.Name;
        }
    }
}
