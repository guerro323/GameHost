using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Audio;
using GameHost.Core.Ecs;
using GameHost.Entities;
using GameHost.Injection;
using SoLoud;

namespace GameHost.Audio
{
    public struct FlatAudioPlayerComponent : IAudioPlayerBackend
    {
        
    }

    public class PlayFlatAudioSystem : AppSystem
    {
        private struct SPlay
        {
            public Entity   resource;
            public float    volume;
            public TimeSpan delay;
        }

        [RestrictToApplication(typeof(GameAudioThreadingHost))]
        private class RestrictedHost : AppSystem
        {
            private SoloudSystem soloudSystem;
            private IManagedWorldTime worldTime;
            
            public ConcurrentQueue<SPlay> plays;

            private List<SPlay> delayedPlays;
            
            public RestrictedHost(WorldCollection collection) : base(collection)
            {
                plays = new ConcurrentQueue<SPlay>();
                delayedPlays = new List<SPlay>();
                
                DependencyResolver.Add(() => ref soloudSystem);
                DependencyResolver.Add(() => ref worldTime);
            }

            protected override void OnUpdate()
            {
                while (plays.TryDequeue(out var playData))
                {
                    if (playData.delay > TimeSpan.Zero)
                    {
                        playData.delay = worldTime.Total.Add(playData.delay - worldTime.Delta);
                        delayedPlays.Add(playData);
                        continue;
                    }

                    var audioHandle = soloudSystem.playPausedGetHandle(playData.resource.Get<Wav>());
                    soloudSystem.soloud.setVolume(audioHandle, 1);
                    soloudSystem.soloud.setPause(audioHandle, 0);
                }

                for (var i = 0; i != delayedPlays.Count; i++)
                {
                    var curr = delayedPlays[i];
                    if (curr.delay > worldTime.Total)
                        continue;

                    var audioHandle = soloudSystem.playPausedGetHandle(curr.resource.Get<Wav>());
                    soloudSystem.soloud.setVolume(audioHandle, 1);
                    soloudSystem.soloud.setPause(audioHandle, 0);

                    // swap back
                    delayedPlays.RemoveAt(i--);
                }
            }
        }

        private RestrictedHost restrictedHost;

        public PlayFlatAudioSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref restrictedHost, new ThreadSystemWithInstanceStrategy<GameAudioThreadingHost>(Context));
        }

        private EntitySet playAudioSet;

        protected override void OnInit()
        {
            base.OnInit();
            playAudioSet = World.Mgr.GetEntities()
                                .With<AudioResource>()
                                .With<FlatAudioPlayerComponent>()
                                .AsSet();
        }

        protected override void OnUpdate()
        {
            foreach (ref readonly var entity in playAudioSet.GetEntities())
            {
                var volume = 1f;
                if (entity.TryGet(out AudioVolumeComponent volumeComponent))
                    volume = volumeComponent.Volume;

                var delay = TimeSpan.Zero;
                if (entity.TryGet(out AudioDelayComponent delayComponent))
                    delay = delayComponent.Delay;
                
                restrictedHost.plays.Enqueue(new SPlay
                {
                    resource = entity.Get<AudioResource>().Source,
                    volume = volume,
                    delay = delay
                });
            }
            
            playAudioSet.DisposeAllEntities();
        }
    }
}
