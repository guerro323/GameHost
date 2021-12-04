using DefaultEcs;
using GameHost.Audio.Players;
using revghost;
using revghost.Ecs;
using revghost.Injection.Dependencies;
using revghost.Utility;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio.Features.Systems;

public class ClientSendAudioResourceSystem : BaseClientAudioSystem
{
    private readonly Dictionary<AudioClientFeature, int> clientLastMaxId;
    private readonly EntitySet resourceSet;

    public ClientSendAudioResourceSystem(Scope scope) : base(scope)
    {
        resourceSet = collection.Mgr.GetEntities()
            .With<AudioResource>()
            .With<IsResourceLoaded<AudioResource>>()
            .AsSet();

        clientLastMaxId = new Dictionary<AudioClientFeature, int>();
    }

    protected override void OnUpdate()
    {
        var maxId = 0;
        foreach (var entity in resourceSet.GetEntities()) maxId = Math.Max(maxId, entity.Get<AudioResource>().Id);

        var update = false;
        var previousId = 0;
        if (!clientLastMaxId.TryGetValue(feature, out var clientMaxId) || clientMaxId < maxId)
        {
            previousId = clientMaxId;
            clientLastMaxId[feature] = maxId;
            update = true;
        }

        if (update)
        {
            var updatedCount = 0;
            foreach (var entity in resourceSet.GetEntities())
                if (entity.Get<AudioResource>().Id > previousId)
                    clientUpdated[updatedCount++] = entity;

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
                    writer.WriteDataSafe(bytesData.Value.AsSpan(), default);
                }
            }

            if (feature.Driver.Broadcast(feature.PreferredChannel, writer.Span) < 0)
                throw new InvalidOperationException("Couldn't send data!");
        }
    }

    protected override void OnInit()
    {
        
    }
}