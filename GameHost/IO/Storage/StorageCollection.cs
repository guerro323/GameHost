using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.IO;

namespace GameHost.IO
{
    public class StorageCollection : IStorage, IEnumerable<IStorage>
    {
        public readonly ReadOnlyCollection<IStorage> AllStorage;

        private readonly List<IStorage> storageList;

        public StorageCollection(IEnumerable<IStorage> storageCollection)
        {
            storageList = storageCollection == null ? new List<IStorage>(1) : new List<IStorage>(storageCollection);
            AllStorage  = new ReadOnlyCollection<IStorage>(storageList);
        }

        public StorageCollection() : this(null)
        {
        }

        public IEnumerator<IStorage> GetEnumerator()
        {
            return storageList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public string CurrentPath { get; }

        public async Task<IEnumerable<IFile>> GetFilesAsync(string pattern)
        {
            var result = new HashSet<IFile>(32);
            foreach (var storage in storageList)
            {
                result.UnionWith((await storage.GetFilesAsync(pattern)).Select(f => new StorageCollectionFileSource(storage, f)));
            }

            return result;
        }

        public async Task<IStorage> GetOrCreateDirectoryAsync(string path)
        {
            var result = new List<IStorage>();
            foreach (var store in storageList)
            {
                try
                {
                    result.Add(await store.GetOrCreateDirectoryAsync(path));
                }
                catch (Exception ex)
                {
                    // todo: we need a better way to display this error, the user may think it would make the application crash
#if DEBUG
                    // Console.WriteLine("Minimal Impact Exception: " + ex);
#endif
                }
            }

            return new ChildStorage(this, new StorageCollection(result));
        }

        public void Add(IStorage storage)
        {
            storageList.Add(storage);
        }

        public override string ToString()
        {
            var start = "StorageCollection {\n";
            foreach (var s in storageList)
            {
                start += "\t" + s + "\n";
            }

            start += "}";
            return start;
        }
    }

    public class StorageCollectionFileSource : IFile
    {
        public readonly IStorage Source;
        public readonly IFile    File;
        public          string   Name     => File.Name;
        public          string   FullName => File.FullName;

        public Task<byte[]> GetContentAsync()
        {
            return File.GetContentAsync();
        }

        public StorageCollectionFileSource(IStorage storage, IFile file)
        {
            Source = storage;
            File   = file;
        }
    }
}