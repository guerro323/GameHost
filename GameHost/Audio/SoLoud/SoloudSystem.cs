using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;

namespace SoLoud
{
    [RestrictToApplication(typeof(GameAudioThreadingHost))]
    public class SoloudSystem : AppSystem
    {
        private readonly Soloud soloud;

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

        public void play(Wav wav)
        {
            soloud.play(wav, 1);
        }
    }
}
