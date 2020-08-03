using System;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Inputs.Components;
using GameHost.Inputs.Features;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.Systems
{
	public class ReceiveInputDataSystem : AppSystemWithFeature<ClientInputFeature>
	{
		private EntitySet              inputSet;
		private InputActionSystemGroup actionSystemGroup;

		public ReceiveInputDataSystem(WorldCollection collection) : base(collection)
		{
			inputSet = collection.Mgr.GetEntities()
			                     .With<InputEntityId>()
			                     .AsSet();

			DependencyResolver.Add(() => ref actionSystemGroup);
		}

		public void ReceiveData(ref DataBufferReader data)
		{
			var systemCount = data.ReadValue<int>();
			for (var i = 0; i != systemCount; i++)
			{
				var actionType = data.ReadString();
				var system     = actionSystemGroup.GetSystemOrDefault(actionType);
				var length     = data.ReadValue<int>();
				if (system == null)
					throw new InvalidOperationException($"System for type '{actionType}' not found!");

				var start = data.CurrReadIndex;
				system.CallDeserialize(ref data);
				if (data.CurrReadIndex != (start + length))
					throw new InvalidOperationException($"Invalid reading for '{actionType}' (expected_length={length}, actual_length={data.CurrReadIndex - start})");
			}
		}

		public void BeginFrame()
		{
			actionSystemGroup.BeginFrame();
		}
	}
}