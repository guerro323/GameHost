using System.Collections.Generic;
using Collections.Pooled;

namespace GameHost.V3.IO.Storage
{
    public interface IStorage
    {
        string CurrentPath { get; }

        void GetFiles<TList>(string pattern, TList listToFill) where TList : IList<IFile>;

        IStorage GetSubStorage(string path);
    }

    public static class StorageExtensions
    {
        public static PooledList<IFile> GetPooledFiles(this IStorage storage, string pattern)
        {
            var list = new PooledList<IFile>();
            storage.GetFiles(pattern, list);
            return list;
        }
    }
}