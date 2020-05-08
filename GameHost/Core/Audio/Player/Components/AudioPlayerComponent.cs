using DefaultEcs;

namespace GameHost.Core.Audio
{
    /// <summary>
    /// The AudioPlayer manage a reference around an audio sample.
    /// </summary>
    public struct AudioPlayerComponent
    {
        /// <summary>
        /// Reference to the audio sample
        /// </summary>
        public Entity Reference;

        /// <summary>
        /// Initialize AudioPlayer with a reference to samples
        /// </summary>
        /// <param name="reference">Audio samples reference</param>
        public AudioPlayerComponent(Entity reference)
        {
            this.Reference = reference;
        }
    }
}
