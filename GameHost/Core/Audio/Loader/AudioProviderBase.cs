using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;

namespace GameHost.Core.Audio.Loader
{
    [RestrictToApplication(typeof(GameAudioThreadingHost))]
    public abstract class AudioProviderBase : AppSystem
    {
        private AudioProviderManager providerMgr;

        public AudioProviderBase(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref providerMgr);
        }

        protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            base.OnDependenciesResolved(dependencies);
            providerMgr.LastProvider = this;
        }


        public abstract Entity LoadAudioFromData(ReadOnlySpan<byte> data);
    }

    [RestrictToApplication(typeof(GameAudioThreadingHost))]
    public class AudioProviderManager : AppSystem
    {
        public AudioProviderManager(WorldCollection collection) : base(collection)
        {
        }

        public AudioProviderBase LastProvider { get; set; }
    }
}
