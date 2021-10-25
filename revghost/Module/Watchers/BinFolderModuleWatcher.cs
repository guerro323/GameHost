using System;
using System.IO;
using DefaultEcs;
using revghost.IO.Storage;

namespace revghost.Module.Watchers;

public class BinFolderModuleWatcher : IPhysicalModuleWatcher
{
    private readonly FileSystemWatcher[] _watchers;
    private readonly Entity _moduleEntity;

    public BinFolderModuleWatcher(Entity moduleEntity, IStorage storage, string fileName)
    {
        using var files = storage.GetPooledFiles($"{fileName}.dll");
        if (files.Count == 0)
            throw new InvalidOperationException($"no '{fileName}.dll' present");

        _watchers = new FileSystemWatcher[files.Count];
        for (var i = 0; i < files.Count; i++)
        {
            _watchers[i] = new FileSystemWatcher(storage.CurrentPath, $"{fileName}.dll")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size
            };
            _watchers[i].Changed += OnFile;
            _watchers[i].EnableRaisingEvents = true;
        }

        _moduleEntity = moduleEntity;

        SetToFront(files[^1].FullName);
    }

    private void OnFile(object sender, FileSystemEventArgs e)
    {
        SetToFront(e.FullPath);
    }

    private void SetToFront(string filePath)
    {
        var file = new LocalFile(new FileInfo(filePath));

        _moduleEntity.Set<IFile>(file);
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers)
            watcher.Changed -= OnFile;
    }
}