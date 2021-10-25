using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using revghost.Utility;

namespace revghost.IO.Storage;

public class DllStorage : IStorage
{
    /// <summary>
    ///     Used for when we instantiate DllStorage as a child.
    /// </summary>
    private string parentPath;

    public DllStorage(Assembly assembly)
    {
        Assembly = assembly;
    }

    public Assembly Assembly { get; }

    public string CurrentPath => new string(DllEmbeddedFile.ToDirectoryLike(Assembly.GetName().Name)) +
                                 (parentPath ?? string.Empty);

    public void GetFiles<TList>(string pattern, TList listToFill) where TList : IList<IFile>
    {
        pattern = pattern.Replace('\\', '/');

        var names = Assembly.GetManifestResourceNames();
        foreach (var mrn in names)
        {
            var file = new DllEmbeddedFile(Assembly, mrn);

            // TODO: optimize it to not alloc a file if the pattern don't match
            if (FileSystemName.MatchesSimpleExpression(CurrentPath + "/" + pattern, file.FullName))
                listToFill.Add(file);
        }
    }

    public IStorage GetSubStorage(string path)
    {
        // remove the last slash if it exist
        while (path[^1] == '/')
            path = path.Substring(0, path.Length - 1);

        var success = Assembly.GetManifestResourceNames().Any(name =>
            DllEmbeddedFile.ToDirectoryLike(name).StartsWith(CurrentPath + "/" + path));
        if (!success)
            throw new InvalidOperationException("No such directory in path: " + path);

        return new DllStorage(Assembly) {parentPath = parentPath + "/" + path};
    }

    public override string ToString()
    {
        return $"DllStorage(Path={CurrentPath})";
    }
}

public class DllEmbeddedFile : IFile
{
    public DllEmbeddedFile(Assembly assembly, string manifestName)
    {
        Assembly = assembly;
        ManifestName = manifestName;
        FullName = new string(ToDirectoryLike(ManifestName, false));
        Name = Path.GetFileName(FullName);
    }

    public Assembly Assembly { get; }

    public string ManifestName { get; }
    public string Name { get; }
    public string FullName { get; }

    public void GetContent<TList>(TList listToFill) where TList : IList<byte>
    {
        using var stream = Assembly.GetManifestResourceStream(ManifestName);
        listToFill.AddRange(stream);
    }

    public async Task GetContentAsync<TList>(TList listToFill) where TList : IList<byte>
    {
        await using var stream = Assembly.GetManifestResourceStream(ManifestName);
        // the -3 +3 is kept for historical reason in comments, since opening .xaml files in visual studio will automatically add a BOM in the beginning of the file...
        // so each time this will happen, seeing this comment will save me hours of pain
        // var mem = new byte[stream.Length /*- 3*/];
        //stream.Position += 3;
        await listToFill.AddRangeAsync(stream);
    }

    public static ReadOnlySpan<char> ToDirectoryLike(string origin, bool ignoreExtension = true)
    {
        var lastDotIndex = ignoreExtension ? origin.Length : origin.LastIndexOf('.');
        var chars = new Span<char>(origin.ToCharArray());
        for (var i = 0; i < lastDotIndex; i++)
            if (chars[i] == '.')
                chars[i] = '/';

        return chars;
    }
}