using System;

namespace GameHost.Core.Audio.Loader.Components
{
    /// <summary>
    /// A reference around an audio samples.
    /// </summary>
    public readonly struct AudioReference : IEquatable<AudioReference>
    {
        /// <summary>
        /// The id of the audio samples.
        /// </summary>
        public readonly uint Id;

        public bool Equals(AudioReference other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is AudioReference other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (int) Id;
        }
    }
}
