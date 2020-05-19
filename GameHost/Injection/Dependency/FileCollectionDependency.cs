using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameHost.Core.IO;

namespace GameHost.Injection.Dependency
{
    public class FileCollectionDependency : DependencyResolver.DependencyBase, DependencyResolver.IResolvedObject
    {
        private readonly string   path;
        private readonly IStorage storage;

        private Task<IEnumerable<IFile>> getFileTask;

        public FileCollectionDependency(string path, IStorage storage)
        {
            this.path    = path;
            this.storage = storage;
        }

        public override void Resolve()
        {
            getFileTask ??= storage.GetFilesAsync(path);

            if (!getFileTask.IsCompleted)
                return;

            Resolved   = getFileTask.Result;
            IsResolved = true;
        }

        public object Resolved { get; private set; }
    }

    public class FileDependency : DependencyResolver.DependencyBase, DependencyResolver.IResolvedObject
    {
        private readonly string   path;
        private readonly IStorage storage;

        private Task<IEnumerable<IFile>> getFileTask;

        public FileDependency(string path, IStorage storage)
        {
            this.path    = path;
            this.storage = storage;
        }

        public override void Resolve()
        {
            getFileTask ??= storage.GetFilesAsync(path);

            if (!getFileTask.IsCompleted)
                return;

            Resolved   = getFileTask.Result.FirstOrDefault();
            IsResolved = true;
        }

        public object Resolved { get; private set; }
    }
}
