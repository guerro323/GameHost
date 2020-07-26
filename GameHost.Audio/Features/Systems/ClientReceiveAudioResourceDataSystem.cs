using System;
using DefaultEcs;
using GameHost.Audio.Players;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.IO;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio.Features.Systems
{
	public class ClientReceiveAudioResourceDataSystem : AppSystemWithFeature<AudioClientFeature>
	{
		private readonly EntitySet resourceSet;

		public ClientReceiveAudioResourceDataSystem(WorldCollection collection) : base(collection)
		{
			resourceSet = collection.Mgr.GetEntities()
			                        .With<AudioResource>()
			                        .With<IsResourceLoaded<AudioResource>>()
			                        .AsSet();
		}

		public void OnMessage(ref DataBufferReader reader)
		{
			var resourceId = reader.ReadValue<int>();
			var wavLength  = reader.ReadValue<double>();

			foreach (var entity in resourceSet.GetEntities())
			{
				if (!entity.TryGet(out AudioResource audioResource))
					continue;
				
				audioResource.Length = TimeSpan.FromSeconds(wavLength);
			}
		}
	}
}