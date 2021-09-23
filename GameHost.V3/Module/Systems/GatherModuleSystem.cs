using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Collections.Pooled;
using DefaultEcs;
using GameHost.V3.Domains.Time;
using GameHost.V3.Ecs;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.IO.Storage;
using GameHost.V3.Loop.EventSubscriber;
using GameHost.V3.Module.Storage;
using GameHost.V3.Module.Watchers;
using GameHost.V3.Utility;

namespace GameHost.V3.Module.Systems
{
    public class GatherModuleSystem : AppSystem
    {
        private readonly Dictionary<string, IPhysicalModuleWatcher> _moduleWatchers = new();

        private World _world;
        private ModuleManager _moduleManager;

        private IModuleCollectionStorage _storage;
        private IDomainUpdateLoopSubscriber _updateLoop;

        public GatherModuleSystem(Scope scope) : base(scope)
        {
            Dependencies.AddRef(() => ref _world);
            Dependencies.AddRef(() => ref _moduleManager);
            Dependencies.AddRef(() => ref _storage);
            Dependencies.AddRef(() => ref _updateLoop);
        }

        private EntitySet _notifySet;

        protected override void OnInit()
        {
            _notifySet = _world.GetEntities()
                .With<RefreshModuleList>()
                .AsSet();

            Disposables.Add(_notifySet);
            Disposables.Add(_updateLoop.Subscribe(OnUpdate));

            // refresh on init
            _world.CreateEntity().Set<RefreshModuleList>();
        }

        private void OnUpdate(WorldTime worldTime)
        {
            if (_notifySet.Count == 0)
                return;

            foreach (var watcher in _moduleWatchers.Values)
                watcher.Dispose();
            _moduleWatchers.Clear();

            using var moduleList = _storage.GetPooledFiles("*/module.config");
            using var bytes = new PooledList<byte>();
            foreach (var moduleFile in moduleList)
            {
                bytes.Clear();
                moduleFile.GetContent(bytes);

                ModuleLoadType? type = null;
                string name = null;
                string author = null;
                string version = null;

                var reader = new Utf8JsonReader(bytes.Span);
                while (reader.Read() && (type == null || name == null || author == null || version == null))
                {
                    if (reader.TokenType != JsonTokenType.PropertyName)
                        continue;

                    var propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName!.Equals("type", StringComparison.InvariantCultureIgnoreCase))
                        type = Enum.Parse<ModuleLoadType>(reader.GetString() ?? "bin", true);
                    if (propertyName!.Equals("name", StringComparison.InvariantCultureIgnoreCase))
                        name = reader.GetString();
                    if (propertyName!.Equals("author", StringComparison.InvariantCultureIgnoreCase))
                        author = reader.GetString();
                    if (propertyName!.Equals("version", StringComparison.InvariantCultureIgnoreCase))
                        version = reader.GetString();
                }

                type ??= ModuleLoadType.Bin;
                name ??= Path.GetFileName(Path.GetDirectoryName(moduleFile.FullName));
                author ??= string.Empty;
                version ??= "0.0";

                Console.WriteLine(
                    $"Detected Module Config! Type={type.Value} Name={name} Author={author} Version={version} Path={moduleFile.FullName}"
                );

                var moduleGroup = Path.GetFileName(Path.GetDirectoryName(moduleFile.FullName));
                var moduleEntity = _moduleManager.GetOrCreate(moduleGroup, string.Empty);
                
                _moduleWatchers[moduleFile.FullName] = type switch
                {
                    ModuleLoadType.Bin => new BinFolderModuleWatcher(
                        moduleEntity,
                        new LocalStorage(Path.GetDirectoryName(moduleFile.FullName) + "/bin"),
                        moduleGroup
                    ),
                    ModuleLoadType.Project => new ProjectFolderModuleWatcher(
                        new LocalStorage(Path.GetDirectoryName(moduleFile.FullName))
                    )
                };
            }

            _notifySet.DisposeAllEntities();
        }

        private enum ModuleLoadType
        {
            // Indicate a module that use precompiled libs (.dlls)
            Bin,
            // Indicate a module that require to be compiled by GameHost (.csproj)
            Project
        }
    }
}