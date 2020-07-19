using System;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;

namespace GameHost.Audio.Features
{
	public struct SPlay
	{
		public float    Volume;
		public TimeSpan Delay;
	}
	
	public class SendRequestToFlatAudioPlayerSystem : AppSystem
	{
		public SendRequestToFlatAudioPlayerSystem(WorldCollection collection) : base(collection)
		{
		}

		protected override void OnUpdate()
		{
			var request = World.Mgr.CreateEntity();
			request.Set(new SPlay
			{
				Volume = 1,
				Delay  = TimeSpan.FromSeconds(0.5)
			});
			request.Set<ClientAudioFeature.SendRequest>();
		}
	}

	public class FlatAudioPlayerSystem : AppSystemWithFeature<IAudioBackendFeature>
	{
		private EntitySet entitySet;
		
		public FlatAudioPlayerSystem(WorldCollection collection) : base(collection)
		{
			entitySet = collection.Mgr.GetEntities()
			                      .With<SPlay>()
			                      .AsSet();
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (var entity in entitySet.GetEntities())
			{
				Console.WriteLine(entity);
			}
			
			entitySet.DisposeAllEntities();
		}
	}
}