namespace GameHost.Audio.Players;

public struct AudioCurrentPlayTime
{
    public TimeSpan Value;

    public AudioCurrentPlayTime(TimeSpan time)
    {
        Value = time;
    }
}