using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using DefaultEcs;
using GameHost.V3.Ecs;
using GameHost.V3.Injection.Dependencies;
using GameHost.V3.IO.Storage;
using GameHost.V3.Threading;
using GameHost.V3.Utility;

namespace GameHost.V3.Module.Systems
{
    public class ModuleManager : AppSystem
    {
        private HostRunnerScope _hostScope;
        private World _world;
        private IScheduler _scheduler;

        public ModuleManager(Scope scope) : base(scope)
        {
            Dependencies.AddRef(() => ref _hostScope);
            Dependencies.AddRef(() => ref _world);
            Dependencies.AddRef(() => ref _scheduler);
        }

        private EntityMap<HostModuleDescription> _moduleSet;

        protected override void OnInit()
        {
            _moduleSet = _world.GetEntities()
                .With<RegisteredModule>()
                .With<HostModuleDescription>()
                .AsMap<HostModuleDescription>();

            Disposables.Add(_moduleSet);
        }

        public Entity GetOrCreate(string moduleGroup, string moduleName)
        {
            var description = new HostModuleDescription(moduleGroup, moduleName);
            if (_moduleSet.TryGetEntity(description, out var moduleEntity))
                return moduleEntity;

            moduleEntity = _world.CreateEntity();
            moduleEntity.Set<RegisteredModule>();
            moduleEntity.Set<ModuleState>();
            moduleEntity.Set(description);

            return moduleEntity;
        }

        public Entity Get(string moduleGroup, string moduleName)
        {
            var description = new HostModuleDescription(moduleGroup, moduleName);
            _moduleSet.TryGetEntity(description, out var moduleEntity);
            return moduleEntity;
        }

        private void UnloadAssembly(Entity entity, out WeakReference weakReference)
        {
            var unloadedEntity = false;
            var asmCtx = entity.Get<AssemblyLoadContext>();
            foreach (var assembly in asmCtx.Assemblies)
            {
                foreach (var other in _world)
                {
                    // This entity (from the function) is also included in the foreach loop
                    if (other.TryGet(out Assembly otherAsm) && otherAsm.Equals(assembly))
                    {
                        if (other.TryGet(out HostModule module))
                        {
                            if (other == entity)
                                unloadedEntity = true;
                            else
                                other.Set(ModuleState.None);

                            module.Dispose();
                            other.Remove<HostModule>();
                        }

                        // Remove anything that could add problems when unloading
                        other.Remove<Assembly>();
                        other.Remove<AssemblyLoadContext>();
                        other.Remove<LoadModuleList>();
                    }
                }
            }
            
            asmCtx.Unload();

            if (!unloadedEntity)
                throw new InvalidOperationException("The entity should have been unloaded");

            weakReference = new WeakReference(asmCtx, true);
        }
        
        public void UnloadModule(Entity module)
        {
            void unloadModuleScheduling((Entity entity, int frameCount, WeakReference weakReference) args)
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
                        _scheduler.Add(unloadModuleScheduling, args);
                        return;
                    }

                    args.entity.Get<ModuleState>() = ModuleState.Zombie;
                    throw new InvalidOperationException(
                        $"Module '{args.entity.Get<HostModuleDescription>().ToPath()}' couldn't be unloaded!"
                    );
                }

                args.entity.Get<ModuleState>() = ModuleState.None;
            }

            Debug.Assert(module.World == _world, "module.World == _world");
            Debug.Assert(module.Has<RegisteredModule>(), "module.Has<RegisteredModule>()");

            Console.WriteLine($"Unloading {module.Get<HostModuleDescription>().ToPath()}");
            
            if (module.Has<AssemblyLoadContext>())
            {
                UnloadAssembly(module, out var weakReference);

                module.Get<ModuleState>() = ModuleState.Unloading;

                _scheduler.Add(unloadModuleScheduling, (module, 100, weakReference), default);
            }
            else
                throw new InvalidOperationException($"no asm ctx for {module.Get<HostModuleDescription>().ToPath()}");
        }

        public HostModule LoadModule(Entity module)
        {
            Debug.Assert(module.World == _world, "module.World == _world");
            Debug.Assert(module.Has<RegisteredModule>(), "module.Has<RegisteredModule>()");

            ref readonly var state = ref module.Get<ModuleState>();
            if (state != ModuleState.None)
                throw new InvalidOperationException($"{module} is not in an unloaded state");

            return CreateModule(module);
        }

        private HostModule CreateModule(Entity entity)
        {
            HostModule module;
            
            // Fast creation path
            if (entity.Has<LoadModuleList>())
            {
                module = entity.Get<LoadModuleList>().List.Last()(_hostScope);
                entity.Set(module.GetType().Assembly);
                entity.Set(module);
                entity.Set(ModuleState.Loaded);

                return module;
            }

            // Slow creation path with reflection and possible new context
            Assembly asm;
            AssemblyLoadContext assemblyLoadContext;
            if (entity.Has<AssemblyLoadContext>())
                assemblyLoadContext = entity.Get<AssemblyLoadContext>();
            else
            {
                assemblyLoadContext = new ModuleAssemblyLoadContext();
                entity.Set(assemblyLoadContext);
            }

            if (entity.Has<AssemblyName>())
                asm = assemblyLoadContext.LoadFromAssemblyName(entity.Get<AssemblyName>());
            else if (entity.Has<IFile>())
                asm = assemblyLoadContext.LoadFromAssemblyPath(entity.Get<IFile>().FullName);
            else
                throw new InvalidOperationException(
                    $"Loading module {entity} but there is neither an {nameof(LoadModuleList)}, {nameof(AssemblyName)} or {nameof(IFile)}!"
                );

            var description = entity.Get<HostModuleDescription>();
            var type = asm.GetType($"{description.Group}.{description.Name}Module", true, true);

            module = (HostModule) Activator.CreateInstance(type!, _hostScope);
            
            entity.Set(asm);
            entity.Set(module);
            entity.Set(ModuleState.Loaded);
            
            return module;
        }
    }
}