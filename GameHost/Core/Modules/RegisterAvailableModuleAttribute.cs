using System;

namespace GameHost.Core.Modules
{
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	public class RegisterAvailableModuleAttribute : Attribute
	{
		public readonly string DisplayName, Author;
		public readonly Type   ModuleType;

		public RegisterAvailableModuleAttribute(string displayName, string author)
		{
			DisplayName = displayName;
			Author      = author;
			ModuleType  = null;
		}

		public RegisterAvailableModuleAttribute(string displayName, string author, Type moduleType)
		{
			DisplayName = displayName;
			Author      = author;
			ModuleType  = moduleType;
		}

		public bool IsValid => ModuleType?.IsSubclassOf(typeof(GameHostModule)) == true;
	}
}