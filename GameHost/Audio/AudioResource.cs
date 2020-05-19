using System;
using System.Linq;
using System.Runtime.CompilerServices;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
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
        private SoloudSystem soloudSystem;
        public LoadAudioResourceSystem(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref soloudSystem, new ThreadSystemWithInstanceStrategy<GameAudioThreadingHost>(Context));
        }

        private EntitySet audioToLoad;

        protected override void OnInit()
        {
            base.OnInit();
            audioToLoad = World.Mgr.GetEntities()
                               .With<AskLoadResource<AudioResource>>()
                               .AsSet();
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
                Console.WriteLine("-----------------------------------");

                Span<byte> fileData = default;
                if (loadByFile)
                {
                    Console.WriteLine("-----------------------------------");
                    var r     = entity.Get<LoadResourceViaFile>();
                    var files = r.Storage.GetFilesAsync(r.Path).Result;
                    if (!files.Any())
                    {
                        Console.WriteLine($"no file found with {r.Path} in storage {r.Storage.CurrentPath}");
                        entity.Dispose();
                        continue;
                    }
                    Console.WriteLine("----------------------------------- 6");

                    var file = files.First();
                    fileData = file.GetContentAsync().Result;
                    Console.WriteLine("----------------------------------- 7");
                }

                lock (soloudSystem.Synchronization)
                {
                    Console.WriteLine("----------------------------------- 8");
                    var resource = soloudSystem.World.Mgr.CreateEntity();
                    var wav      = new Wav();
                    wav.loadMem((IntPtr)Unsafe.AsPointer(ref fileData.GetPinnableReference()), (uint)fileData.Length, aCopy: 1);
                    resource.Set(wav);

                    soloudSystem.play(wav);

                    entity.Set<AudioResource>(new AudioResource {Source = resource});
                    entity.Remove<AskLoadResource<AudioResource>>();
                    Console.WriteLine("-----------------------------------dadakldkamlkmlkamdamkkaokdop 9");
                }
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
