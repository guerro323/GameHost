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
using GameHost.Core.Ecs;
using GameHost.Core.Features.Systems;
using GameHost.Core.IO;
using GameHost.Core.Threading;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace GameHost.Core.Modules.Feature
{
    [RestrictToApplication(typeof(ExecutiveEntryApplication))]
    public class ModuleManager : AppSystemWithFeature<ModuleLoaderFeature>
    {
        public Dictionary<string, GameHostModule> ModuleMap;

        private AssemblyLoadContext loadContext;
        private ILogger             logger;
        private IScheduler          scheduler;

        public ModuleManager(WorldCollection collection) : base(collection)
        {
            DependencyResolver.Add(() => ref loadContext);
            DependencyResolver.Add(() => ref logger);
            DependencyResolver.Add(() => ref scheduler);
        }

        protected override void OnFeatureAdded(ModuleLoaderFeature obj)
        {
            if (ModuleMap != null)
                throw new Exception("yooooooo");

            ModuleMap = new Dictionary<string, GameHostModule>(8);

            // Add existing assemblies to modules...
            var moduleAsmBag = new ConcurrentBag<(Assembly, GameHostModuleDescription)>();
            Parallel.ForEach(AppDomain.CurrentDomain.GetAssemblies(), asm =>
            {
                RegisterAvailableModuleAttribute attr;
                if ((attr = asm.GetCustomAttribute<RegisterAvailableModuleAttribute>()) != null)
                {
                    moduleAsmBag.Add((asm, new GameHostModuleDescription
                    {
                        DisplayName = attr.DisplayName,
                        Author      = attr.Author,
                        NameId      = asm.GetName().Name
                    }));
                }
            });
            while (moduleAsmBag.TryTake(out var tuple))
            {
                var (asm, smod) = tuple;

                var rm = World.Mgr.CreateEntity();
                rm.Set(new RegisteredModule {State = ModuleState.Loaded, Description = smod});
                // these components are used for when the assembly get unloaded and if we need them back...
                rm.Set(asm);
                rm.Set(asm.GetName());
                rm.Set(loadContext);

                LoadModule(rm);
            }
        }

        private void CheckEntity(Entity entity)
        {
            Debug.Assert(entity.World == World.Mgr, "entity.World == World.Mgr");
            Debug.Assert(entity.Has<RegisteredModule>(), "entity.Has<RegisteredModule>()");
        }

        private Assembly GetAssembly(Entity entity, out AssemblyLoadContext asmLoadCtx)
        {
            asmLoadCtx = loadContext;
            if (entity.Has<Assembly>())
                return entity.Get<Assembly>();

            if (entity.Has<AssemblyLoadContext>())
                asmLoadCtx = entity.Get<AssemblyLoadContext>();
            else
            {
                asmLoadCtx = new ModuleAssemblyLoadContext();
                entity.Set<AssemblyLoadContext>(asmLoadCtx);
            }

            if (entity.Has<AssemblyName>())
                return asmLoadCtx.LoadFromAssemblyName(entity.Get<AssemblyName>());
            if (entity.Has<IFile>())
                return asmLoadCtx.LoadFromAssemblyPath(entity.Get<IFile>().FullName);
            Console.WriteLine(":(");

            return null;
        }

        public void LoadModule(Entity entity)
        {
            CheckEntity(entity);

            ref var module = ref entity.Get<RegisteredModule>();
            Debug.Assert(!ModuleMap.ContainsKey(module.Description.NameId), "!ModuleMap.ContainsKey(module.Info.NameId)");

            module.State = ModuleState.IsLoading;

            var asm = GetAssembly(entity, out var asmLoadCtx);
            if (asm == null)
                throw new FileLoadException("could not load module: " + module.Description.NameId);

            var attr = asm.GetCustomAttribute<RegisterAvailableModuleAttribute>();
            if (attr == null)
                throw new InvalidOperationException($"The assembly '{asm}' is not a valid module.");

            module.Description.Author = attr.Author;

            var cmodType = attr.IsValid ? attr.ModuleType : typeof(GameHostModule);
            var cmod     = Activator.CreateInstance(cmodType, entity, Context, module.Description);

            ModuleMap[module.Description.NameId] = (GameHostModule) cmod;
            entity.Set(asm);
            entity.Set((GameHostModule) cmod);

            module.State = ModuleState.Loaded;
        }

        private void UnloadAssembly(Entity entity, out WeakReference weakReference)
        {
            var asmCtx = entity.Get<AssemblyLoadContext>();
            foreach (var assembly in asmCtx.Assemblies)
            {
                foreach (var other in World.Mgr)
                {
                    // This entity (from the function) is also included in the foreach loop
                    if (other.TryGet(out Assembly otherAsm) && otherAsm.Equals(assembly))
                    {
                        if (other.TryGet(out GameHostModule cmod))
                        {
                            cmod.Dispose();
                            other.Remove<GameHostModule>();
                        }

                        // Remove anything that could add problems when unloading
                        other.Remove<Assembly>();
                        other.Remove<AssemblyLoadContext>();
                    }
                }
            }

            weakReference = new WeakReference(asmCtx, true);
        }

        private void unloadModuleScheduling((Entity entity, int frameCount, WeakReference weakReference) args)
        {
            Debug.Assert(args.weakReference != null, "args.weakReference != null");
            
            for (var i = 0; i < 2 && args.weakReference.IsAlive; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            if (args.weakReference.IsAlive)
            {
                if (args.frameCount-- > 0)
                {
                    scheduler.Schedule(unloadModuleScheduling, args, default);
                    return;
                }

                args.entity.Get<RegisteredModule>().State = ModuleState.Zombie;
                throw new InvalidOperationException($"Module '{args.entity.Get<RegisteredModule>().Description.NameId}' couldn't be unloaded!");
            }

            args.entity.Get<RegisteredModule>().State = ModuleState.None;
            logger.ZLogInformation($"{args.entity.Get<RegisteredModule>().Description.NameId} module unloaded!");
        }

        public void UnloadModule(Entity entity)
        {
            CheckEntity(entity);

            ref var module = ref entity.Get<RegisteredModule>();
            Debug.Assert(ModuleMap.ContainsKey(module.Description.NameId), "ModuleMap.ContainsKey(module.Info.NameId)");

            if (entity.Has<AssemblyLoadContext>())
            {
                ModuleMap.Remove(module.Description.NameId);
                
                UnloadAssembly(entity, out var weakReference);

                module.State = ModuleState.Unloading;
                
                scheduler.Schedule(unloadModuleScheduling, (entity, 100, weakReference), default);
            }
        }

        public GameHostModule GetModule(string moduleName)
        {
            return ModuleMap[moduleName];
        }

        public GameHostModule GetModule(Assembly assembly) => GetModule(assembly.GetName().Name);

        public T GetModule<T>(string   moduleName) where T : GameHostModule => (T) GetModule(moduleName);
        public T GetModule<T>(Assembly assembly) where T : GameHostModule   => (T) GetModule(assembly);
    }
}