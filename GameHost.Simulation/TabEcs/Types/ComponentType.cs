using System;
using System.Reflection;
using NetFabric.Hyperlinq;

namespace GameHost.Simulation.TabEcs
{
	public readonly struct ComponentType : IEquatable<ComponentType>
	{
		public readonly uint Id;

		public ComponentType(uint id)
		{
			Id = id;
		}

		public bool Equals(ComponentType other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is ComponentType other && Equals(other);
		}

		public override int GetHashCode()
		{
			return (int) Id;
		}

		public static bool operator ==(ComponentType left, ComponentType right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ComponentType left, ComponentType right)
		{
			return !left.Equals(right);
		}
	}
	
	public static class ComponentTypeUtility
	{
		// https://stackoverflow.com/a/27851610
		public static bool IsZeroSizeStruct(Type t)
		{
			return t.IsValueType && !t.IsPrimitive &&
			       t.GetFields((BindingFlags)0x34).All(fi => IsZeroSizeStruct(fi.FieldType));
		}
	}
}