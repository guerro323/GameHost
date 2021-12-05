using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.Zip;
using revghost.Shared;
using revghost.Utility;

namespace revghost.IO.Storage;

/// <summary>
/// Represent an entry from a zip file
/// </summary>
/// <remarks>
/// The zip is only opened when <see cref="GetContentAsync"/> is called (and then closed when that call has been completed)
/// </remarks>
public class ZipEntryFile : IFile
{
    private readonly IFile _zipFile;
    
    public string Name { get; }
    public string FullName { get; }
    
    public ZipEntryFile(IFile zipFile, ZipEntry entry)
    {
        this._zipFile = zipFile;
            
        Name     = Path.GetFileName(entry.Name);
        FullName = entry.Name;
    }

    public void GetContent<TList>(TList listToFill) where TList : IList<byte>
    {
        using var list = _zipFile.GetPooledBytes();
        using var _ = DisposableArray<byte>.Rent(list.Count, out var bytes);
        {
            list.CopyTo(bytes.AsSpan(0, list.Count));
        }

        using var zipStream = new MemoryStream(bytes, 0, list.Count);
        using var archive = new ZipFile(zipStream);

        var entry = archive.GetEntry(FullName);

        var outputStream = archive.GetInputStream(entry);

        var mem = new byte[entry.Size];
        outputStream.Read(mem);
        listToFill.AddRange(mem);
    }

    public async Task GetContentAsync<TList>(TList listToFill) where TList : IList<byte>
    {
        using var list = _zipFile.GetPooledBytes();
        using var _ = DisposableArray<byte>.Rent(list.Count, out var bytes);
        {
            list.CopyTo(bytes.AsSpan(0, list.Count));
        }

        using var zipStream = new MemoryStream(bytes, 0, list.Count);
        using var archive = new ZipFile(zipStream);

        var entry = archive.GetEntry(FullName);

        await using var outputStream = archive.GetInputStream(entry);
        var mem = new byte[entry.Size];
        await outputStream.ReadAsync(mem);

        listToFill.AddRange(mem);
    }
}