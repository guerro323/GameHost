using System;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Inputs.Components;
using GameHost.Inputs.Features;
using GameHost.Inputs.Layouts;
using NetFabric.Hyperlinq;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.Systems
{
	public class SendPushedInputLayoutsToBackend : AppSystemWithFeature<ClientInputFeature>
	{
		private EntitySet withoutIdSet;
		private EntitySet pushInputSet;

		private int maxId = 1;

		public SendPushedInputLayoutsToBackend(WorldCollection collection) : base(collection)
		{
			withoutIdSet = collection.Mgr.GetEntities()
			                         .With<PushInputLayoutChange>()
			                         .With<InputActionLayouts>()
			                         .Without<InputEntityId>()
			                         .AsSet();
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

			// assign an Input Id to the entities for the backend to track them.
			if (withoutIdSet.Count > 0)
			{
				foreach (var entity in withoutIdSet.GetEntities().ToArray())
					entity.Set(new InputEntityId(maxId++));
			}

			var data       = new DataBufferWriter(0);
			var entitySpan = pushInputSet.GetEntities();

			Console.WriteLine("send data!");
			data.WriteInt((int) EMessageInputType.Register);
			data.WriteInt(entitySpan.Length);
			Console.WriteLine(data.Length);
			foreach (ref readonly var entity in entitySpan)
			{
				var layouts = entity.Get<InputActionLayouts>();
				data.WriteInt(entity.Get<InputEntityId>().Value);
				data.WriteInt(layouts.Count);
				foreach (var layout in layouts.Values)
				{
					data.WriteStaticString(layout.GetType().FullName);
					data.WriteStaticString(layout.Id);

					var skipMarker = data.WriteInt(0);
					layout.Serialize(ref data);
					data.WriteInt(data.Length - skipMarker.GetOffset(sizeof(int)).Index, skipMarker);
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