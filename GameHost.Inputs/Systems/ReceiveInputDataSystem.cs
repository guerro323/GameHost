using System;
using DefaultEcs;
using GameHost.Applications;
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
				var system     = actionSystemGroup.TryGetSystem(actionType);
				if (system == null)
					throw new InvalidOperationException($"System for type '{actionType}' not found!");

				system.CallDeserialize(ref data);
			}
		}

		public void BeginFrame()
		{
			actionSystemGroup.BeginFrame();
		}
	}
}