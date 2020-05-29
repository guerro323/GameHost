using System;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Entities;
using GameHost.Injection;

namespace SoLoud
{
    [RestrictToApplication(typeof(GameAudioThreadingHost))]
    public class SoloudSystem : AppSystem
    {
        public readonly Soloud soloud;

        [DependencyStrategy]
        public IManagedWorldTime wt { get; set; }

        public SoloudSystem(WorldCollection collection) : base(collection)
        {
            soloud = new Soloud();
            soloud.init();
        }

        public override void Dispose()
        {
            base.Dispose();
            soloud.deinit();
        }

        private int playCount;
        public void play(Wav wav)
        {
            var handle = soloud.play(wav, 1, aPaused: 0);
            //soloud.setDelaySamples(handle, (uint)(soloud.getSamplerate(handle) * 0.5));
            soloud.scheduleStop(handle, wav.getLength());
        }
        
        public uint playPausedGetHandle(Wav wav)
        {
            var handle = soloud.play(wav, 1, aPaused: 1);
            return handle;
        }
    }
}
