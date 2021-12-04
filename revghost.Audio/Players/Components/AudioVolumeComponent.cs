namespace GameHost.Audio.Players;

/// <summary>
///     Manage the volume of an <see cref="IAudioPlayerComponent" />
/// </summary>
public struct AudioVolumeComponent
{
	/// <summary>
	///     The... volume.
	/// </summary>
	public float Volume;

	/// <summary>
	///     Initialize AudioVolume with the volume
	/// </summary>
	/// <param name="volume">The volume between [0..+infinity]</param>
	/// <exception cref="InvalidOperationException">The volume was under 0</exception>
	public AudioVolumeComponent(float volume)
    {
        if (volume < 0)
            throw new InvalidOperationException("you can not create a volume under 0");

        Volume = volume;
    }
}