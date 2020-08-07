using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Audio.Players;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.IO;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio.Features.Systems
{
	public class ClientSendAudioResourceSystem : AppSystemWithFeature<AudioClientFeature>
	{
		private readonly EntitySet resourceSet;
		private readonly Dictionary<AudioClientFeature, int> clientLastMaxId;

		public ClientSendAudioResourceSystem(WorldCollection collection) : base(collection)
		{
			resourceSet = collection.Mgr.GetEntities()
			                        .With<AudioResource>()
			                        .With<IsResourceLoaded<AudioResource>>()
			                        .AsSet();

			clientLastMaxId = new Dictionary<AudioClientFeature, int>();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			var maxId = 0;
			foreach (var entity in resourceSet.GetEntities())
			{
				maxId = Math.Max(maxId, entity.Get<AudioResource>().Id);
			}

			// for memory usage, don't put this call into the foreach since stackalloc is only freed when this method itself is finished
			Span<Entity> clientUpdated = stackalloc Entity[resourceSet.Count];
			foreach (var feature in Features)
			{
				var update     = false;
				var previousId = 0;
				if (!clientLastMaxId.TryGetValue(feature, out var clientMaxId) || clientMaxId < maxId)
				{
					previousId               = clientMaxId;
					clientLastMaxId[feature] = maxId;
					update                   = true;
				}
				
				if (update)
				{
					var updatedCount = 0;
					foreach (var entity in resourceSet.GetEntities())
					{
						if (entity.Get<AudioResource>().Id > previousId)
							clientUpdated[updatedCount++] = entity;
					}

					using var writer = new DataBufferWriter(updatedCount);
					writer.WriteInt((int) EAudioSendType.RegisterResource);
					writer.WriteInt(updatedCount);
					foreach (var entity in clientUpdated.Slice(0, updatedCount))
					{
						writer.WriteInt(entity.Get<AudioResource>().Id);
						var typeMarker = writer.WriteInt(0);
						if (entity.TryGet(out AudioBytesData bytesData))
						{
							writer.WriteInt((int) EAudioRegisterResourceType.Bytes, typeMarker);
							writer.WriteInt(bytesData.Value.Length);
							unsafe
							{
								fixed (byte* bytePtr = bytesData.Value)
								{
									writer.WriteDataSafe(bytePtr, bytesData.Value.Length, default);
								}
							}
						}
					}
					
					if (feature.Driver.Broadcast(feature.PreferredChannel, writer.Span) < 0)
						throw new InvalidOperationException("Couldn't send data!");
				}
			}
		}
	}
}