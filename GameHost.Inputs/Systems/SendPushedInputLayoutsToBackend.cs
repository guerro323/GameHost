using System;
using System.Collections.Generic;
using DefaultEcs;
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

		private readonly Dictionary<ClientInputFeature, int> clientLastMaxId;

		public SendPushedInputLayoutsToBackend(WorldCollection collection) : base(collection)
		{
			pushInputSet = collection.Mgr.GetEntities()
			                         .With<InputActionLayouts>()
			                         .With<InputEntityId>()
			                         .AsSet();
			
			clientLastMaxId = new Dictionary<ClientInputFeature, int>();
		}

		public override bool CanUpdate()
		{
			return pushInputSet.Count > 0 && base.CanUpdate();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			
			var maxId = 0;
			foreach (var entity in pushInputSet.GetEntities())
			{
				maxId = Math.Max(maxId, entity.Get<InputEntityId>().Value);
			}
			
			var entitySpan = pushInputSet.GetEntities();
			if (entitySpan.Length == 0)
				return;
			
			foreach (var feature in Features)
			{
				var update     = false;
				foreach (var ent in entitySpan)
					if (ent.Has<PushInputLayoutChange>())
					{
						update = true;
						break;
					}
				
				if (!clientLastMaxId.TryGetValue(feature, out var clientMaxId) || clientMaxId < maxId)
				{
					clientLastMaxId[feature] = maxId;
					update                   = true;
				}
				
				if (update)
				{
					var data = new DataBufferWriter(0);
					try
					{
						data.WriteInt((int) EMessageInputType.Register);
						data.WriteInt(entitySpan.Length);
						foreach (ref readonly var entity in entitySpan)
						{
							var layouts = entity.Get<InputActionLayouts>();
							data.WriteInt(entity.Get<InputEntityId>().Value);
							data.WriteStaticString(entity.Get<InputActionType>().Type.FullName);

							var skipActionMarker = data.WriteInt(0);

							data.WriteInt(layouts.Count);
							foreach (var layout in layouts.Values)
							{
								data.WriteStaticString(layout.Id);
								data.WriteStaticString(layout.GetType().FullName);

								var skipLayoutMarker = data.WriteInt(0);
								layout.Serialize(ref data);
								data.WriteInt(data.Length - skipLayoutMarker.Index, skipLayoutMarker);
							}

							data.WriteInt(data.Length - skipActionMarker.Index, skipActionMarker);
						}
						
						if (feature.Driver.Broadcast(feature.PreferredChannel, data.Span) < 0)
							throw new InvalidOperationException("Couldn't send data!");
					}
					finally
					{
						data.Dispose();
					}
				}
			}

			pushInputSet.Remove<PushInputLayoutChange>();
		}
	}
}