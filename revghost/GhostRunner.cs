using System;
using System.Runtime.Loader;
using DefaultEcs;
using revghost.Domains;
using revghost.Injection;
using revghost.IO.Storage;
using revghost.Module.Storage;
using revghost.Shared.Threading.Schedulers;
using revghost.Threading;
using revghost.Utility;

namespace revghost;

public class GhostRunner : IDisposable
{
    public readonly ExecutiveDomain Domain;
    public readonly HostRunnerScope Scope;

    public Entity HostEntity => Domain.DomainEntity;

    public GhostRunner()
    {
        Scope = new HostRunnerScope();
        Domain = new ExecutiveDomain(this);
    }

    public void Dispose()
    {
        HostLogger.Output.Info("Disposing GhostRunner");
        
        Domain.Dispose();
        Scope.Dispose();

        foreach (var ctx in AssemblyLoadContext.All)
        {
            if (!ctx.IsCollectible)
            {
                HostLogger.Output.Info(
                    $"Not possible to unload assembly context '{ctx.Name}' because it wasn't collectible. Contains:\n" +
                    $"{string.Join(",\n", ctx.Assemblies)}");
                continue;
            }

            try
            {
                HostLogger.Output.Info(
                    $"Trying to unload assembly context '{ctx.Name}' at the runner disposal. Contains:\n" +
                    $"{string.Join(",\n", ctx.Assemblies)}");
                
                ctx.Unload();
            }
            catch (Exception ex)
            {
                HostLogger.Output.Info(
                    $"(!) Couldn't unload an assembly context '{ctx.Name}' at the runner disposal! Contains:\n" +
                    $"{string.Join(",\n", ctx.Assemblies)}");
            }
        }
    }

    public bool Loop()
    {
        return Domain.Loop();
    }
}

public class HostRunnerScope : Scope
{
    public readonly World World;

    public ExecutiveStorage ExecutiveStorage
    {
        get
        {
            if (!Context.TryGet(out ExecutiveStorage storage))
                throw new NullReferenceException(nameof(ExecutiveStorage));
            return storage;
        }

        set => Context.Register(value);
    }
        
    public IStorage UserStorage
    {
        get
        {
            if (!Context.TryGet(out IStorage storage))
                throw new NullReferenceException(nameof(UserStorage));
            return storage;
        }

        set => Context.Register(value);
    }
        
    public IModuleCollectionStorage ModuleStorage
    {
        get
        {
            if (!Context.TryGet(out IModuleCollectionStorage storage))
                throw new NullReferenceException(nameof(ModuleStorage));
            return storage;
        }

        set => Context.Register(value);
    }
        
    public IDependencyResolver DependencyResolver
    {
        get
        {
            if (!Context.TryGet(out IDependencyResolver resolver))
                throw new NullReferenceException(nameof(DependencyResolver));
            return resolver;
        }

        set => Context.Register(value);
    }

    public IScheduler Scheduler
    {
        get
        {
            if (!Context.TryGet(out IScheduler scheduler))
                throw new NullReferenceException(nameof(Scheduler));
            return scheduler;
        }

        set => Context.Register(value);
    }

    public HostRunnerScope()
    {
        World = new World();
        Context.Register(World);

        if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
        {
            ExecutiveStorage = new ExecutiveStorage(new LocalStorage(AppContext.BaseDirectory));
            UserStorage = new LocalStorage(AppContext.BaseDirectory);
            ModuleStorage = new ModuleCollectionStorage(UserStorage.GetSubStorage("Modules"));
        }

        Scheduler = new ConcurrentScheduler();
        {
            DependencyResolver = new SchedulerDependencyResolver(Scheduler);
        }
    }

    public override void Dispose()
    {
        base.Dispose();

        World.Dispose();
    }
}