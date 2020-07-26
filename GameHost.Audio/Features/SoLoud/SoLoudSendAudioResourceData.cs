using GameHost.Audio.Features;
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio
{
	public class SoLoudSendAudioResourceData : AppSystemWithFeature<SoLoudBackendFeature>
	{
		public SoLoudSendAudioResourceData(WorldCollection collection) : base(collection)
		{
		}

		public void Send(TransportConnection connection, int resource, ref Wav wav)
		{
			using var writer = new DataBufferWriter(0);
			writer.WriteInt((int) EAudioSendType.SendReplyResourceData);
			writer.WriteInt(resource);
			writer.WriteValue((double) wav.getLength());

			foreach (var feature in Features)
			{
				feature.Driver.Send(default, connection, writer.Span);
			}
		}
	}
}