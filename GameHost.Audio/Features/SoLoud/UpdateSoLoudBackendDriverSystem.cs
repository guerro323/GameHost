using System;
using GameHost.Audio.Applications;
using GameHost.Audio.Features;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using GameHost.Native.Char;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio
{
	[RestrictToApplication(typeof(AudioApplication))]
	public class UpdateSoLoudBackendDriverSystem : AppSystemWithFeature<IAudioBackendFeature>
	{
		private SoLoudResourceManager resourceManager;
		private SoLoudPlayerManager   playerManager;

		public UpdateSoLoudBackendDriverSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref resourceManager);
			DependencyResolver.Add(() => ref playerManager);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (var (_, feature) in Features)
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
							break;
						case TransportEvent.EType.Disconnect:
							break;
						case TransportEvent.EType.Data:
							var reader = new DataBufferReader(ev.Data);
							var type = (EAudioSendType) reader.ReadValue<int>();
							switch (type)
							{
								case EAudioSendType.Unknown:
									break;
								case EAudioSendType.RegisterResource:
									OnReceiveResource(ev.Connection, ref reader);
									break;
								case EAudioSendType.RegisterPlayer:
									OnReceivePlayer(ev.Connection, ref reader);
									break;
								case EAudioSendType.SendAudioPlayerData:
									var player    = reader.ReadBuffer<CharBuffer128>();
									var @delegate = playerManager.GetDelegate(player);
									@delegate?.Invoke(ev.Connection, ref reader);
									break;
								default:
									throw new ArgumentOutOfRangeException($"EAudioSendType: " + type);
							}

							break;
						default:
							throw new ArgumentOutOfRangeException($"MessageType" + ev.Type);
					}
				}
			}
		}

		private void OnReceiveResource(TransportConnection connection, ref DataBufferReader reader)
		{
			var count = reader.ReadValue<int>();
			for (var i = 0; i != count; i++)
			{
				var id   = reader.ReadValue<int>();
				var type = (EAudioRegisterResourceType) reader.ReadValue<int>();
				switch (type)
				{
					case EAudioRegisterResourceType.Bytes:
					{
						var data = new byte[reader.ReadValue<int>()];
						reader.ReadDataSafe(data.AsSpan());

						resourceManager.Register(connection, id, data);

						break;
					}
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		private void OnReceivePlayer(TransportConnection connection, ref DataBufferReader reader)
		{
			var count = reader.ReadValue<int>();
			for (var i = 0; i != count; i++)
			{
				var id   = reader.ReadValue<int>();
				var type = reader.ReadString();

				playerManager.Register(connection, id, type);
			}
		}
	}
}