using System;

namespace revghost.Module;

public readonly struct HostModuleDescription : IEquatable<HostModuleDescription>
{
    public readonly string Group;
    public readonly string Name;

    public HostModuleDescription(string group, string name)
    {
        Group = group;
        Name = name;
    }

    public string ToPath() => $"{Group}/{Name}";

    /// <summary>
    /// Get whether or not a string match this description
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Match(ReadOnlySpan<char> other)
    {
        if (other.IsEmpty)
            return false;

        // Group = Hello
        // Name = World

        // other = Hello
        if (other.SequenceEqual(Group))
            return Name.Length == 0;

        // other = Hello/
        if (other[^1] == '/' && other[..^1].SequenceEqual(Group))
            return Name.Length == 0;

        // other = Hello/World
        // other = Hello/WorldModule
        var slashIdx = other.IndexOf('/');
        if (slashIdx <= 0)
            return false;

        var moduleIdx = other.IndexOf("module", StringComparison.InvariantCultureIgnoreCase);
        if (moduleIdx < 0)
        {
            if (Name.Length == 0 && other.Length > slashIdx)
                return false;

            moduleIdx = Name.Length;
        }
        else
        {
            moduleIdx -= "module".Length;
        }

        var groupSpan = Group.AsSpan();
        var nameSpan = Name.AsSpan();
        return groupSpan.SequenceEqual(other.Slice(0, slashIdx))
               && nameSpan.SequenceEqual(other.Slice(slashIdx + 1, moduleIdx));
    }

    public bool IsPathValid()
    {
        static bool isStrInvalid(string str)
        {
            return str.Contains('/')
                   || str.Contains('\\')
                   || str.Contains('?')
                   || str.Contains(':')
                   || str.Contains('|')
                   || str.Contains('*')
                   || str.Contains('<')
                   || str.Contains('>');
        }

        return isStrInvalid(Group) && (string.IsNullOrEmpty(Name) || isStrInvalid(Name));
    }

    public bool Equals(HostModuleDescription other)
    {
        return Group.Equals(other.Group) && Name.Equals(other.Name);
    }

    public override bool Equals(object obj)
    {
        return obj is HostModuleDescription other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Group, Name);
    }
}