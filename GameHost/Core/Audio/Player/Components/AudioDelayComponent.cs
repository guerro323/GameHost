using System;

namespace GameHost.Core.Audio
{
    /// <summary>
    /// Manage the delay of an <see cref="AudioPlayerUtility"/>
    /// </summary>
    public struct AudioDelayComponent
    {
        public TimeSpan Delay;

        public AudioDelayComponent(TimeSpan delay)
        {
            if (delay < TimeSpan.Zero)
                delay = TimeSpan.Zero;
            this.Delay = delay;
        }
    }
}
