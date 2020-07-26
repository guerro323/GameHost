using System;
using System.Collections.Generic;
using System.Linq;
using DefaultEcs;
using GameHost.Audio.Players;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.IO;

namespace GameHost.Audio.Systems
{
	public class LoadAudioResourceSystem : AppSystem
	{
		private readonly struct Key__ : IEquatable<Key__>
		{
			public readonly IFile    File;
			public readonly string   Path;
			public readonly IStorage Storage;

			public Key__(IFile file, string path, IStorage storage)
			{
				File    = file;
				Path    = path;
				Storage = storage;
			}

			public bool Equals(Key__ other)
			{
				return File == other.File && Path == other.Path && Equals(Storage, other.Storage);
			}

			public override bool Equals(object obj)
			{
				return obj is Key__ other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(File, Path, Storage);
			}

			public static bool operator ==(Key__ left, Key__ right)
			{
				return left.Equals(right);
			}

			public static bool operator !=(Key__ left, Key__ right)
			{
				return !left.Equals(right);
			}
		}

		private Dictionary<Key__, Entity> resourceMap;
		private int                       currentId;

		private EntitySet toLoadSet;

		public LoadAudioResourceSystem(WorldCollection collection) : base(collection)
		{
			resourceMap = new Dictionary<Key__, Entity>();
			currentId   = 1;

			toLoadSet = World.Mgr.GetEntities()
			                 .With<AskLoadResource<AudioResource>>()
			                 .AsSet();
		}

		protected override void OnUpdate()
		{
			Span<Entity> entities = stackalloc Entity[toLoadSet.Count];
			toLoadSet.GetEntities().CopyTo(entities);
			foreach (ref var entity in entities)
			{
				Span<byte> fileData = default;
				if (entity.Has<LoadResourceViaStorage>())
				{
					var r     = entity.Get<LoadResourceViaStorage>();
					var files = r.Storage.GetFilesAsync(r.Path).Result;
					if (!files.Any())
					{
						Console.WriteLine($"no file found with {r.Path} in storage {r.Storage}");
						entity.Dispose();
						continue;
					}

					var file = files.First();
					// todo: async
					entity.Set(new AudioBytesData {Value = file.GetContentAsync().Result});
				}
				else if (entity.Has<LoadResourceViaFile>())
				{
					// todo: async
					entity.Set(new AudioBytesData
					{
						Value = entity
						        .Get<LoadResourceViaFile>().File
						        .GetContentAsync().Result
					});
				}
				else
				{
					continue;
				}

				entity.Set(new AudioResource {Id = currentId++});
				entity.Set(new IsResourceLoaded<AudioResource>());
				entity.Remove<AskLoadResource<AudioResource>>();
			}
		}

		public ResourceHandle<AudioResource> Load(string path, IStorage storage)
		{
			var key = new Key__(null, path, storage);
			if (!resourceMap.TryGetValue(key, out var resourceEntity))
			{
				resourceMap[key] = resourceEntity = World.Mgr.CreateEntity();
				resourceEntity.Set(new AskLoadResource<AudioResource>());
				resourceEntity.Set(new LoadResourceViaStorage {Path = path, Storage = storage});
			}

			return new ResourceHandle<AudioResource>(resourceEntity);
		}

		public ResourceHandle<AudioResource> Load(IFile file)
		{
			var key = new Key__(file, null, null);
			if (!resourceMap.TryGetValue(key, out var resourceEntity))
			{
				resourceMap[key] = resourceEntity = World.Mgr.CreateEntity();
				resourceEntity.Set(new AskLoadResource<AudioResource>());
				resourceEntity.Set(new LoadResourceViaFile {File = file});
			}

			return new ResourceHandle<AudioResource>(resourceEntity);
		}
	}
}