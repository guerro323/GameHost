﻿using GameHost.Core.IO;

namespace GameHost.Audio.Features;

public enum EAudioSendType
{
    Unknown = 0,
    RegisterResource = 1,
    RegisterPlayer = 2,
    SendReplyResourceData = 3,
    SendAudioPlayerData = 10
}

public enum EAudioRegisterResourceType
{
    Unknown = 0,
    Bytes = 1,
    File = 2
}

/// <summary>
///     Represent an audio backend
/// </summary>
public interface IAudioBackendFeature
{
    public TransportDriver Driver { get; }
    public TransportAddress TransportAddress { get; }
}

/// <summary>
///     A feature that send data to <see cref="IAudioBackendFeature" />
/// </summary>
public class AudioClientFeature
{
    public AudioClientFeature(TransportDriver driver, TransportChannel channel)
    {
        Driver = driver;
        PreferredChannel = channel;
    }

    public TransportDriver Driver { get; }
    public TransportChannel PreferredChannel { get; }
}