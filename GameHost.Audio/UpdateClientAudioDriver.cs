using System;
using System.Buffers;
using DefaultEcs;
using GameHost.Audio.Features;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio
{
	public class UpdateClientAudioDriver : AppSystemWithFeature<ClientAudioFeature>
	{
		private EntitySet requestSet;

		public UpdateClientAudioDriver(WorldCollection collection) : base(collection)
		{
			requestSet = collection.Mgr.GetEntities()
			                       .With<DataBufferWriter>()
			                       .With<ClientAudioFeature.SendRequest>()
			                       .AsSet();
		}

		protected override void OnUpdate()
		{
			// Update first...
			foreach (var feature in Features)
			{
				feature.Driver.Update();

				while (feature.Driver.Accept().IsCreated)
				{
				}

				// todo: check events for errors and all
				while (feature.Driver.PopEvent().Type != TransportEvent.EType.None)
				{
				}
			}

			// Send data...
			foreach (ref readonly var entity in requestSet.GetEntities())
			{
				var data = entity.Get<DataBufferWriter>();

				foreach (var feature in Features)
				{
					unsafe
					{
						feature.Driver.Broadcast(feature.PreferredChannel, new ReadOnlySpan<byte>((void*) data.GetSafePtr(), data.Length));
					}
				}

				data.Dispose();
			}
			
			requestSet.DisposeAllEntities();
		}
	}
}