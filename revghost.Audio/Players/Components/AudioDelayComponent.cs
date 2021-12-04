namespace GameHost.Audio.Players;

/// <summary>
///     Manage the delay of an <see cref="IAudioPlayerComponent" />
/// </summary>
public struct AudioDelayComponent
{
    public TimeSpan Delay;

    public AudioDelayComponent(TimeSpan delay)
    {
        if (delay < TimeSpan.Zero)
            delay = TimeSpan.Zero;
        Delay = delay;
    }
}