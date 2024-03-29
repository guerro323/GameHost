using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using DefaultEcs;
using revghost.Ecs;
using revghost.Injection.Dependencies;
using revghost.IO.Storage;
using revghost.Shared.Threading.Schedulers;
using revghost.Threading;
using revghost.Utility;

namespace revghost.Module.Systems;

public class ModuleManager : AppSystem
{
    private HostRunnerScope _hostScope;
    private World _world;
    private IScheduler _scheduler;

    private readonly HostLogger _logger = new HostLogger(nameof(ModuleManager));

    public ModuleManager(Scope scope) : base(scope)
    {
        Dependencies.Add(() => ref _hostScope);
        Dependencies.Add(() => ref _world);
        Dependencies.Add(() => ref _scheduler);
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

    protected override void OnDispose()
    {
        base.OnDispose();

        _scheduler = new ConcurrentScheduler();
        foreach (var module in _moduleSet.Keys)
        {
            var ent = _moduleSet[module];
            // skip ghost module
            if (module.Group == "GameHost" && module.Name == "Entry")
                continue;
            // skip main module
            if (!ent.Has<AssemblyLoadContext>())
                continue;

            if (ent.Get<ModuleState>() == ModuleState.Loaded)
            {
                UnloadModule(ent);
            }
        }

        for (var i = 0; i < 150; i++)
        {
            try
            {
                ((ConcurrentScheduler) _scheduler).Run();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString(), "final_unload");
            }
        }
        
        ((ConcurrentScheduler) _scheduler).Dispose();
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

                args.entity.Get<ModuleState>() = ModuleState.None; // Don't set to zombie

                HostLogger.Output.Error(
                    $"Module '{args.entity.Get<HostModuleDescription>().ToPath()}' couldn't be unloaded!",
                    "ModuleManager",
                    "module-unload-final"
                );
            }

            args.entity.Get<ModuleState>() = ModuleState.None;
        }

        Debug.Assert(module.World == _world, "module.World == _world");
        Debug.Assert(module.Has<RegisteredModule>(), "module.Has<RegisteredModule>()");
            
        _logger.Info(
            $"Unloading {module.Get<HostModuleDescription>().ToPath()} (entity={module})",
            "module-unload"
        );
            
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

        _logger.Info(
            $"Loading {module.Get<HostModuleDescription>().ToPath()} (entity={module})",
            "module-load"
        );
            
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
            HostLogger.Output.Info($"Loading module {entity.Get<HostModuleDescription>().ToPath()} via fast path", "module-load");
            
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
        {
            HostLogger.Output.Info($"Module {entity.Get<HostModuleDescription>().ToPath()} set with existing assembly context", "module-load");
            assemblyLoadContext = entity.Get<AssemblyLoadContext>();
        }
        else
        {
            HostLogger.Output.Info($"Module {entity.Get<HostModuleDescription>().ToPath()} set with new assembly context", "module-load");
            
            assemblyLoadContext = new ModuleAssemblyLoadContext();
            entity.Set(assemblyLoadContext);
        }

        if (entity.Has<AssemblyName>())
        {
            HostLogger.Output.Info($"Loading module {entity.Get<HostModuleDescription>().ToPath()} via AssemblyName", "module-load");
            asm = assemblyLoadContext.LoadFromAssemblyName(entity.Get<AssemblyName>());
        }
        else if (entity.Has<IFile>())
        {   
            HostLogger.Output.Info($"Loading module {entity.Get<HostModuleDescription>().ToPath()} via file '{entity.Get<IFile>().FullName}'", "module-load");
            using var bytes = entity.Get<IFile>().GetPooledBytes();
            using var stream = new MemoryStream(bytes.ToArray());

            var file = entity.Get<IFile>();
            var resolving = (AssemblyLoadContext context, AssemblyName name) =>
            {
                var directory = Path.GetDirectoryName(file.FullName);
                var filename = name.Name.Split(',')[0] + ".dll".ToLower();
                var asmFile = Path.Combine(directory, filename);

                var resolver = new AssemblyDependencyResolver(file.FullName);
                var asmPath = resolver.ResolveAssemblyToPath(name);
                
                HostLogger.Output.Info("Resolved to path:  " + asmPath);

                try
                {
                    var bytes = File.ReadAllBytes(asmPath);
                    using var stream = new MemoryStream(bytes);
                    
                    var asm = context.LoadFromStream(stream);
                    return asm;
                }
                catch (Exception ex)
                {
                    HostLogger.Output.Error(
                        $"Couldn't load directly {filename} in phase 1",
                        "ModuleManager",
                        "module-load");

                    return null;
                }
            };
            assemblyLoadContext.Resolving += resolving;
            
            asm = assemblyLoadContext.LoadFromStream(stream);
        }
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