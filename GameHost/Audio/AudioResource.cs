using System;
using System.Linq;
using System.Runtime.CompilerServices;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Audio.Loader;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Core.Threading;
using GameHost.Injection;
using GameHost.IO;
using SoLoud;

namespace GameHost.Audio
{
    public class AudioResource : Resource
    {
        internal Entity Source;
    }

    public class LoadAudioResourceSystem : AppSystem
    {
        private AudioProviderManager providerMgr;
        public LoadAudioResourceSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref providerMgr, new ThreadSystemWithInstanceStrategy<GameAudioThreadingHost>(Context));
        }

        private EntitySet audioToLoad;

        protected override void OnInit()
        {
            base.OnInit();
            audioToLoad = World.Mgr.GetEntities()
                               .With<AskLoadResource<AudioResource>>()
                               .AsSet();
        }

        public override bool CanUpdate()
        {
            return base.CanUpdate() && providerMgr.LastProvider != null;
        }

        protected override unsafe void OnUpdate()
        {
            base.OnUpdate();

            Span<Entity> entities = stackalloc Entity[audioToLoad.Count];
            audioToLoad.GetEntities().CopyTo(entities);
            foreach (ref var entity in entities)
            {
                var loadByFile = entity.Has<LoadResourceViaFile>();
                if (!loadByFile)
                    continue;

                Span<byte> fileData = default;
                if (loadByFile)
                {
                    var r     = entity.Get<LoadResourceViaFile>();
                    var files = r.Storage.GetFilesAsync(r.Path).Result;
                    if (!files.Any())
                    {
                        Console.WriteLine($"no file found with {r.Path} in storage {r.Storage.CurrentPath}");
                        entity.Dispose();
                        continue;
                    }

                    var file = files.First();
                    fileData = file.GetContentAsync().Result;
                }

                using var treadLocker = ThreadingHost.Synchronize<GameAudioThreadingHost>();
                entity.Set(new AudioResource {Source = providerMgr.LastProvider.LoadAudioFromData(fileData)});
                entity.Remove<AskLoadResource<AudioResource>>();
            }
        }

        public ResourceHandle<AudioResource> Start(string path, IStorage storage)
        {
            var ent = World.Mgr.CreateEntity();
            ent.Set(new AskLoadResource<AudioResource>());
            ent.Set(new LoadResourceViaFile {Path = path, Storage = storage});

            return new ResourceHandle<AudioResource>(ent);
        }
    }
}
