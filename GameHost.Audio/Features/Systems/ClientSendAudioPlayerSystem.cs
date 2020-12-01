using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Audio.Players;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using RevolutionSnapshot.Core.Buffers;
using StormiumTeam.GameBase.Utility.Misc;

namespace GameHost.Audio.Features.Systems
{
	public class ClientSendAudioPlayerSystem : AppSystemWithFeature<AudioClientFeature>
	{
		private          EntitySet withoutIdSet;
		private readonly EntitySet playerSet;

		private          int                                 selfLastMaxId;
		private readonly Dictionary<AudioClientFeature, int> clientLastMaxId;

		public ClientSendAudioPlayerSystem(WorldCollection collection) : base(collection)
		{
			withoutIdSet = collection.Mgr.GetEntities()
			                         .With<AudioPlayerType>()
			                         .Without<AudioPlayerId>()
			                         .AsSet();

			playerSet = collection.Mgr.GetEntities()
			                      .With<AudioPlayerType>()
			                      .With<AudioPlayerId>()
			                      .AsSet();

			selfLastMaxId   = 1;
			clientLastMaxId = new Dictionary<AudioClientFeature, int>();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			if (withoutIdSet.Count > 0)
			{
				Span<Entity> entities = stackalloc Entity[withoutIdSet.Count];
				withoutIdSet.GetEntities().CopyTo(entities);
				foreach (ref readonly var entity in entities)
				{
					entity.Set(new AudioPlayerId(selfLastMaxId++));
				}
			}

			var maxId = 0;
			foreach (var entity in playerSet.GetEntities())
			{
				maxId = Math.Max(maxId, entity.Get<AudioPlayerId>().Id);
			}

			// for memory usage, don't put this call into the foreach since stackalloc is only freed when this method itself is finished
			Span<Entity> clientUpdated = stackalloc Entity[playerSet.Count];
			foreach (var (featureEntity, feature) in Features)
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
					foreach (var entity in playerSet.GetEntities())
					{
						if (entity.Get<AudioPlayerId>().Id > previousId)
							clientUpdated[updatedCount++] = entity;
					}

					using var writer = new DataBufferWriter(updatedCount);
					writer.WriteInt((int) EAudioSendType.RegisterPlayer);
					writer.WriteInt(updatedCount);
					foreach (var entity in clientUpdated.Slice(0, updatedCount))
					{
						writer.WriteInt(entity.Get<AudioPlayerId>().Id);
						writer.WriteStaticString(TypeExt.GetFriendlyName(entity.Get<AudioPlayerType>().Type));
					}
					
					if (feature.Driver.Broadcast(feature.PreferredChannel, writer.Span) < 0)
						throw new InvalidOperationException("Couldn't send data!");
				}
			}
		}
	}
}