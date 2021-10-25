using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using revghost.Utility;

namespace revghost.IO.Storage;

public class LocalStorage : IStorage
{
    private readonly DirectoryInfo directory;

    public LocalStorage(DirectoryInfo directory, bool create = true)
    {
        if (!directory.Exists)
            directory.Create();

        this.directory = directory;
    }

    public LocalStorage(string directory, bool create = true) : this(new DirectoryInfo(directory), create)
    {
    }

    public string CurrentPath => directory.FullName;

    public void GetFiles<TList>(string pattern, TList listToFill) where TList : IList<IFile>
    {
        if (!directory.Exists)
            return;

        var recursive = pattern.StartsWith("*/") || pattern.StartsWith("*\\");
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        pattern = pattern[(recursive ? 2 : 0)..];
            
        foreach (var file in directory.GetFiles(pattern, option))
        {
            listToFill.Add(new LocalFile(file));
        }
    }

    public IStorage GetSubStorage(string path)
    {
        return new LocalStorage(directory.CreateSubdirectory(path));
    }

    public override string ToString()
    {
        return $"LocalStorage(Path={CurrentPath})";
    }
}

public class LocalFile : IWriteFile
{
    private readonly FileInfo file;

    public LocalFile(FileInfo file)
    {
        this.file = file;
    }

    public string Name => file.Name;
    public string FullName => file.FullName;

    public void GetContent<TList>(TList listToFill) where TList : IList<byte>
    {
        using var stream = File.Open(FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        listToFill.AddRange(stream);
    }

    public async Task GetContentAsync<TList>(TList listToFill) where TList : IList<byte>
    {
        await using var stream = File.Open(FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
        await listToFill.AddRangeAsync(stream);
    }

    public Task WriteContentAsync(byte[] content)
    {
        // TODO: Async
        File.WriteAllBytes(FullName, content);
        return Task.CompletedTask;
    }
}