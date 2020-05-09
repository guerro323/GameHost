using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                result.UnionWith(await storage.GetFilesAsync(pattern));
            }

            return result;
        }

        public Task<IStorage> GetOrCreateDirectoryAsync(string path)
        {
            throw new NotImplementedException("StorageCollection does not yet implement GetOrCreateDirectoryAsync()");
        }

        public void Add(IStorage storage)
        {
            storageList.Add(storage);
        }
    }
}
