using System;
using System.Collections.Concurrent;
using System.Threading;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Entities;
using GameHost.Injection;
using SoLoud;

namespace GameHost.Audio
{
    public struct PlayFlatAudioComponent
    {
        
    }

    public class PlayFlatAudioSystem : AppSystem
    {
        private struct SPlay
        {
            public Entity resource;
            public float volume;
        }

        [RestrictToApplication(typeof(GameAudioThreadingHost))]
        private class RestrictedHost : AppSystem
        {
            private SoloudSystem soloudSystem;
            
            public ConcurrentQueue<SPlay> plays;
            
            public RestrictedHost(WorldCollection collection) : base(collection)
            {
                plays = new ConcurrentQueue<SPlay>();
                
                DependencyResolver.Add(() => ref soloudSystem);
            }

            protected override void OnUpdate()
            {
                while (plays.TryDequeue(out var playData))
                {
                    soloudSystem.play(playData.resource.Get<Wav>());
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
                                .With<PlayFlatAudioComponent>()
                                .AsSet();
        }

        protected override void OnUpdate()
        {
            foreach (ref readonly var entity in playAudioSet.GetEntities())
            {
                Console.WriteLine("play : " + entity);
                restrictedHost.plays.Enqueue(new SPlay
                {
                    resource = entity.Get<AudioResource>().Source
                });
            }
            
            playAudioSet.DisposeAllEntities();
        }
    }
}
