using System;
using System.Buffers;
using System.Runtime.InteropServices;
using DefaultEcs;
using DefaultEcs.Serialization;
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
				var serializer = new BinarySerializer();
				var buffer     = new DataBufferWriter(0);
				using (var stream = new DataStreamWriter(buffer))
				{
					serializer.Serialize(stream, entity);
				}

				foreach (var feature in Features)
				{
					unsafe
					{
						feature.Driver.Broadcast(feature.PreferredChannel, new Span<byte>((void*) buffer.GetSafePtr(), buffer.Length));
					}
				}

				buffer.Dispose();
			}

			requestSet.DisposeAllEntities();
		}
	}
}