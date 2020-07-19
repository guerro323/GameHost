using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using DefaultEcs;
using GameHost.Audio.Features;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio
{
	public class UpdateAudioBackendDriverSystem : AppSystemWithFeature<IAudioBackendFeature>
	{
		private TransportableData transportableData;
		
		public UpdateAudioBackendDriverSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref transportableData);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (var feature in Features)
			{
				feature.Driver.Update();

				while (feature.Driver.Accept().IsCreated)
				{
				}

				TransportEvent ev;
				while ((ev = feature.Driver.PopEvent()).Type != TransportEvent.EType.None)
				{
					switch (ev.Type)
					{
						case TransportEvent.EType.None:
							break;
						case TransportEvent.EType.RequestConnection:
							break;
						case TransportEvent.EType.Connect:
							Console.WriteLine("connection!");
							break;
						case TransportEvent.EType.Disconnect:
							break;
						case TransportEvent.EType.Data:
							Console.WriteLine("data!");
							var reader = new DataBufferReader(ev.Data);
							transportableData.Deserialize(ref reader);
							
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}
	}
}