using System;
using System.Reflection;
using NetFabric.Hyperlinq;

namespace GameHost.Simulation.TabEcs
{
	public readonly struct ComponentType
	{
		public readonly uint Id;

		public ComponentType(uint id)
		{
			Id = id;
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