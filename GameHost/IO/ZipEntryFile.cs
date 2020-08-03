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
        private readonly IFile zipFile;

        public string Name     { get; }
        public string FullName { get; }

        public async Task<byte[]> GetContentAsync()
        {
            await using var zipStream = new MemoryStream(await zipFile.GetContentAsync());
            using var       archive   = new ZipFile(zipStream);
            var             entry     = archive.GetEntry(FullName);

            await using var outputStream = archive.GetInputStream(entry);

            var mem = new byte[entry.Size];
            await outputStream.ReadAsync(mem, 0, mem.Length);

            return mem;
        }

        public ZipEntryFile(IFile zipFile, ZipEntry entry)
        {
            this.zipFile = zipFile;
            
            Name     = Path.GetFileName(entry.Name);
            FullName = entry.Name;
        }
    }
}
