using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace GameHost.Core.IO
{
    /// <summary>
    /// Represent any type of resource
    /// </summary>
    public interface IResource
    {
    }

    /// <summary>
    /// Represent a storage that can contains files and sub-storage.
    /// </summary>
    public interface IStorage : IResource
    {
        /// <summary>
        /// The current path of the storage, may be null.
        /// </summary>
        [CanBeNull]
        string CurrentPath { get; }

        /// <summary>
        /// Return all files under a specific pattern.
        /// </summary>
        /// <param name="pattern">The pattern</param>
        /// <returns>A collection files.</returns>
        Task<IEnumerable<IFile>> GetFilesAsync(string pattern);

        /// <summary>
        /// Get or create a directory at a relative path.
        /// </summary>
        /// <param name="path">Relative Path</param>
        /// <returns>A storage under this path.</returns>
        Task<IStorage> GetOrCreateDirectoryAsync(string path);
    }

    /// <summary>
    /// Represent a file
    /// </summary>
    public interface IFile : IResource
    {
        /// <summary>
        /// The name of the file with extension
        /// </summary>
        string Name { get; }

        /// <summary>
        /// The full path to this file
        /// </summary>
        string FullName { get; }

        /// <summary>
        /// Get a file content in bytes
        /// </summary>
        /// <param name="path">Relative Path</param>
        /// <returns>File content in bytes</returns>
        Task<byte[]> GetContentAsync();
    }

    public interface IWriteFile : IFile
    {
        Task WriteContentAsync(byte[] content);
    }
}