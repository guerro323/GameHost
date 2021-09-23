using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GameHost.V3.IO.Storage
{
    public class MultiStorage : IStorage, IEnumerable<IStorage>
    {
        public readonly ReadOnlyCollection<IStorage> AllStorage;

        private readonly List<IStorage> storageList;

        public MultiStorage(IEnumerable<IStorage> storageCollection)
        {
            storageList = storageCollection == null ? new List<IStorage>(1) : new List<IStorage>(storageCollection);
            AllStorage = new ReadOnlyCollection<IStorage>(storageList);
        }

        public MultiStorage() : this(null)
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

        public void GetFiles<TList>(string pattern, TList listToFill) where TList : IList<IFile>
        {
            var result = new HashSet<IFile>(storageList.Count);
            foreach (var storage in storageList)
            {
                storage.GetFiles(pattern, listToFill);
                result.UnionWith(listToFill);
                listToFill.Clear();
            }

            foreach (var r in result)
                listToFill.Add(r);
        }

        public IStorage GetSubStorage(string path)
        {
            var result = new List<IStorage>();
            foreach (var store in storageList)
                try
                {
                    result.Add(store.GetSubStorage(path));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            return new ChildStorage(this, new MultiStorage(result));
        }

        public void Add(IStorage storage)
        {
            storageList.Add(storage);
        }

        public override string ToString()
        {
            var start = "StorageCollection {\n";
            foreach (var s in storageList) start += "\t" + s + "\n";

            start += "}";
            return start;
        }
    }
}