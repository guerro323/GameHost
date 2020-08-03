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

	/*public readonly struct InputActionSerializeFunction
	{
		public delegate void OnSerialize(ref DataBufferWriter buffer);

		public readonly OnSerialize Value;

		public InputActionSerializeFunction(OnSerialize value)
		{
			Value = value;
		}
	}
	
	public readonly struct InputActionDeserializeFunction
	{
		public delegate void OnDeserialize(Entity entity, ref DataBufferReader buffer);

		public readonly OnDeserialize Value;

		public InputActionDeserializeFunction(OnDeserialize value)
		{
			Value = value;
		}
	}*/
}