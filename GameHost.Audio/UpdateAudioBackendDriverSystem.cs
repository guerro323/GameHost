using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
	public class UpdateAudioBackendDriverSystem : AppSystemWithFeature<IAudioBackendFeature>
	{
		private BinarySerializer serializer;

		public UpdateAudioBackendDriverSystem(WorldCollection collection) : base(collection)
		{
			serializer = new BinarySerializer();
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
							using (var stream = new MemoryStream(ev.Data.ToArray()))
								serializer.Deserialize(stream, World.Mgr);

							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}
	}
}