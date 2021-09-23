using System.Collections.Generic;
using System.Threading.Tasks;
using Collections.Pooled;

namespace GameHost.V3.IO.Storage
{
    /// <summary>
    ///     Represent a file
    /// </summary>
    public interface IFile
    {
        /// <summary>
        ///     The name of the file with extension
        /// </summary>
        string Name { get; }

        /// <summary>
        ///     The full path to this file
        /// </summary>
        string FullName { get; }

        /// <summary>
        ///     Get a file content in bytes
        /// </summary>
        void GetContent<TList>(TList listToFill) where TList : IList<byte>;

        Task GetContentAsync<TList>(TList listToFill) where TList : IList<byte>;
    }

    public interface IWriteFile : IFile
    {
        Task WriteContentAsync(byte[] content);
    }
    
    public static class FileExtensions
    {
        /// <summary>
        /// Shortcut method for getting the bytes of a file
        /// </summary>
        /// <returns>A pooled collection that the caller have responsibility to free</returns>
        public static PooledList<byte> GetPooledBytes(this IFile file)
        {
            var bytes = new PooledList<byte>();
            file.GetContent(bytes);
            return bytes;
        }
    }
}