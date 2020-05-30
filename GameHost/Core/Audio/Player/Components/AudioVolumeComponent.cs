using System;

namespace GameHost.Core.Audio
{
    /// <summary>
    /// Manage the volume of an <see cref="AudioPlayerUtility"/>
    /// </summary>
    public struct AudioVolumeComponent
    {
        /// <summary>
        /// The... volume.
        /// </summary>
        public float Volume;

        /// <summary>
        /// Initialize AudioVolume with the volume
        /// </summary>
        /// <param name="volume">The volume between [0..+infinity]</param>
        /// <exception cref="InvalidOperationException">The volume was under 0</exception>
        public AudioVolumeComponent(float volume)
        {
            if (volume < 0)
                throw new InvalidOperationException("you can not create a volume under 0");

            this.Volume = volume;
        }
    }
}
