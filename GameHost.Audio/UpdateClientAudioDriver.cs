using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using DefaultEcs;
using DefaultEcs.Serialization;
using GameHost.Audio.Features;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio
{
	public class UpdateClientAudioDriver : AppSystemWithFeature<AudioClientFeature>
	{
		public UpdateClientAudioDriver(WorldCollection collection) : base(collection)
		{
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
				TransportEvent ev;
				while ((ev = feature.Driver.PopEvent()).Type != TransportEvent.EType.None)
				{
					Console.WriteLine("CLIENT " + ev.Type);
					switch (ev.Type)
					{
						case TransportEvent.EType.None:
							break;
						case TransportEvent.EType.RequestConnection:
							break;
						case TransportEvent.EType.Connect:
							break;
						case TransportEvent.EType.Disconnect:
							break;
						case TransportEvent.EType.Data:
							var reader = new DataBufferReader(ev.Data);
							var type = (EAudioSendType) reader.ReadValue<int>();
							switch (type)
							{
								case EAudioSendType.Unknown:
									throw new InvalidOperationException();
								case EAudioSendType.RegisterResource:
									throw new InvalidOperationException("shouldn't be called");
								case EAudioSendType.SendAudioPlayerData:
									throw new InvalidOperationException("shouldn't be called");
								default:
									throw new ArgumentOutOfRangeException();
							}
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}
				}
			}
		}
	}
}