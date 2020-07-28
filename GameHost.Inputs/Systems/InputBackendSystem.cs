using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Inputs.Components;
using GameHost.Inputs.Layouts;

namespace GameHost.Inputs.Systems
{
	public struct ReplicatedInputAction : IEquatable<ReplicatedInputAction>
	{
		public TransportConnection Connection;
		public int                 Id;

		public bool Equals(ReplicatedInputAction other)
		{
			return Connection.Equals(other.Connection) && Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is ReplicatedInputAction other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Connection, Id);
		}

		public static bool operator ==(ReplicatedInputAction left, ReplicatedInputAction right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(ReplicatedInputAction left, ReplicatedInputAction right)
		{
			return !left.Equals(right);
		}
	}
	
	public class InputControl
	{
		public bool wasPressedThisFrame;
		public bool wasReleasedThisFrame;
		public bool isPressed;
		
		public float axisValue;

		public float ReadValue()
		{
			return axisValue;
		}
	}
	
	public class InputBackendSystem : AppSystem
	{
		public Dictionary<ReplicatedInputAction, Entity>             ghIdToEntityMap  = new Dictionary<ReplicatedInputAction, Entity>();
		public Dictionary<ReplicatedInputAction, InputActionLayouts> ghIdToLayoutsMap = new Dictionary<ReplicatedInputAction, InputActionLayouts>();
		
		public Dictionary<string, InputControl> inputDataMap;
		
		public InputBackendSystem(WorldCollection collection) : base(collection)
		{
			inputDataMap = new Dictionary<string,InputControl>();
		}
		
		public InputControl GetOrCreateInputControl(string path)
		{
			path = path.ToLower();
			
			if (!inputDataMap.TryGetValue(path, out var control))
				inputDataMap[path] = control = new InputControl();
			return control;
		}

		public InputControl GetInputControl(string path)
		{
			return inputDataMap[path.ToLower()];
		}

		public InputActionLayouts GetLayoutsOf(Entity entity)
		{
			if (!entity.TryGet(out ReplicatedInputAction inputAction))
				throw new InvalidOperationException($"GetLayoutsOf: {entity} should be an input action");

			return ghIdToLayoutsMap[inputAction];
		}
	}
}