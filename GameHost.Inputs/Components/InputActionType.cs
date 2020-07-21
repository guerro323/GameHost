using System;

namespace GameHost.Inputs.Components
{
	public struct InputActionType
	{
		public readonly Type Type;

		public InputActionType(Type type)
		{
			Type = type;
		}
	}
}