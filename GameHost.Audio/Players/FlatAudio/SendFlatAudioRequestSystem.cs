using System;
using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Audio.Features
{
	public class SendFlatAudioRequestSystem : AppSystem
	{
		public SendFlatAudioRequestSystem(WorldCollection collection) : base(collection)
		{
		}

		protected override void OnUpdate()
		{
			var request = World.Mgr.CreateEntity();
			/*request.Set(MessagePackSerializer.Serialize(new SPlay
			{
				Volume = 1,
				Delay  = TimeSpan.FromSeconds(0.5)
			}));*/
			request.Set<ClientAudioFeature.SendRequest>();
		}
	}

	public struct SPlay
	{
		public float Volume;
		public TimeSpan Delay;
	}
}