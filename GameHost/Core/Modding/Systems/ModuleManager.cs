using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Core.Modding.Components;
using GameHost.Injection;

namespace GameHost.Core.Modding.Systems
{
    public class ModuleManager : AppSystem
    {
        [RestrictToApplication(typeof(MainThreadHost))]
        private class RestrictedHostSystem : AppSystem
        {
            public Dictionary<string, CModule> ModuleMap;

            [DependencyStrategy]
            public AssemblyLoadContext LoadContext { get; set; }

            protected override void OnInit()
            {
                base.OnInit();
                ModuleMap = new Dictionary<string, CModule>(8);

                // Add existing assemblies to modules...
                var moduleAsmBag = new ConcurrentBag<(Assembly, SModuleInfo)>();
                Parallel.ForEach(AppDomain.CurrentDomain.GetAssemblies(), asm =>
                {
                    ModuleDescriptionAttribute attr;
                    if ((attr = asm.GetCustomAttribute<ModuleDescriptionAttribute>()) != null)
                    {
                        moduleAsmBag.Add((asm, new SModuleInfo {DisplayName = attr.DisplayName, Author = attr.Author, NameId = asm.GetName().Name}));
                    }
                });
                while (moduleAsmBag.TryTake(out var tuple))
                {
                    var (asm, smod) = tuple;

                    var rm = World.Mgr.CreateEntity();
                    rm.Set(new RegisteredModule {State = ModuleState.Loaded, Info = smod});
                    // these components are used for when the assembly get unloaded and if we need them back...
                    rm.Set(asm);
                    rm.Set(asm.GetName());

                    LoadModule(rm);
                }
            }

            private void CheckEntity(Entity entity)
            {
                Debug.Assert(entity.World == World.Mgr, "entity.World == World.Mgr");
                Debug.Assert(entity.Has<RegisteredModule>(), "entity.Has<RegisteredModule>()");
            }

            private Assembly GetAssembly(Entity entity)
            {
                if (entity.Has<Assembly>())
                    return entity.Get<Assembly>();
                if (entity.Has<AssemblyName>())
                    return LoadContext.LoadFromAssemblyName(entity.Get<AssemblyName>());
                if (entity.Has<IFile>())
                    return LoadContext.LoadFromAssemblyPath(entity.Get<IFile>().FullName);
                Console.WriteLine(":(");

                return null;
            }

            public void LoadModule(Entity entity)
            {
                CheckEntity(entity);

                ref var module = ref entity.Get<RegisteredModule>();
                Debug.Assert(!ModuleMap.ContainsKey(module.Info.NameId), "!ModuleMap.ContainsKey(module.Info.NameId)");

                module.State = ModuleState.IsLoading;

                var asm = GetAssembly(entity);
                if (asm == null)
                    throw new FileLoadException("could not load module: " + module.Info.NameId);

                var attr = asm.GetCustomAttribute<ModuleDescriptionAttribute>();
                if (attr == null)
                    throw new InvalidOperationException($"The assembly '{asm}' is not a valid module.");

                module.Info.Author = attr.Author;

                var cmodType = attr.IsValid ? attr.ModuleType : typeof(CModule);
                var cmod     = Activator.CreateInstance(cmodType, entity, Context, module.Info);

                ModuleMap[module.Info.NameId] = (CModule)cmod;
                entity.Set(asm);
                entity.Set(cmod);
            }

            public void UnloadModule(Entity entity)
            {
                CheckEntity(entity);

                ref var module = ref entity.Get<RegisteredModule>();
                Debug.Assert(ModuleMap.ContainsKey(module.Info.NameId), "ModuleMap.ContainsKey(module.Info.NameId)");

                module.State = ModuleState.Unloading;

                if (entity.Has<Assembly>())
                    entity.Remove<Assembly>();
            }
        }

        private MainThreadClient           client;
        private Lazy<RestrictedHostSystem> hostSystem;

        protected override void OnInit()
        {
            base.OnInit();
            client = new MainThreadClient();
            client.Connect();

            hostSystem = new Lazy<RestrictedHostSystem>(() =>
            {
                using (client.SynchronizeThread())
                    return client.Listener.WorldCollection.GetOrCreate<RestrictedHostSystem>();
            });
        }

        public void LoadModule(Entity entity)
        {
            lock (hostSystem.Value)
            {
                hostSystem.Value.LoadModule(entity);
            }
        }

        public void UnloadModule(Entity entity)
        {
            lock (hostSystem.Value)
            {
                hostSystem.Value.UnloadModule(entity);
            }
        }

        public CModule GetModule(string moduleName)
        {
            lock (hostSystem.Value)
            {
                return hostSystem.Value.ModuleMap[moduleName];
            }
        }

        public CModule GetModule(Assembly assembly) => GetModule(assembly.GetName().Name);

        public T GetModule<T>(string   moduleName) where T : CModule => (T)GetModule(moduleName);
        public T GetModule<T>(Assembly assembly) where T : CModule   => (T)GetModule(assembly);
    }
}
