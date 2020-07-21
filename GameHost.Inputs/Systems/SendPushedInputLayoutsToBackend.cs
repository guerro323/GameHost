using System;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Inputs.Components;
using GameHost.Inputs.Features;
using GameHost.Inputs.Layouts;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.Systems
{
	public class SendPushedInputLayoutsToBackend : AppSystemWithFeature<ClientInputFeature>
	{
		private EntitySet pushInputSet;

		public SendPushedInputLayoutsToBackend(WorldCollection collection) : base(collection)
		{
			pushInputSet = collection.Mgr.GetEntities()
			                         .With<PushInputLayoutChange>()
			                         .With<InputActionLayouts>()
			                         .AsSet();
		}

		public override bool CanUpdate()
		{
			return pushInputSet.Count > 0 && base.CanUpdate();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			var data       = new DataBufferWriter(0);
			var entitySpan = pushInputSet.GetEntities();

			data.WriteInt((int) EMessageInputType.Register);
			data.WriteInt(entitySpan.Length);
			foreach (ref readonly var entity in entitySpan)
			{
				var layouts = entity.Get<InputActionLayouts>();
				data.WriteInt(layouts.Count);
				foreach (var layout in layouts.Values)
				{
					data.WriteStaticString(layout.Id);
					layout.Serialize(ref data);
				}
			}

			foreach (var feature in Features)
			{
				unsafe
				{
					feature.Driver.Broadcast(feature.PreferredChannel, new Span<byte>((void*) data.GetSafePtr(), data.Length));
				}
			}

			data.Dispose();

			pushInputSet.Remove<PushInputLayoutChange>();
		}
	}
}