using System;

namespace GameHost.V3.Module
{
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

        public bool IsValid()
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
}