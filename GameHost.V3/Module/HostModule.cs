using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using DefaultEcs;
using GameHost.V3.Domains;
using GameHost.V3.Injection;
using GameHost.V3.IO.Storage;
using GameHost.V3.Module.Storage;
using GameHost.V3.Module.Systems;
using GameHost.V3.Utility;

namespace GameHost.V3.Module
{
    public abstract partial class HostModule : IDisposable, IHasDependencies
    {
        public readonly string Group;
        public readonly string Name;

        public readonly DependencyCollection Dependencies;

        protected readonly IList<IDisposable> Disposables = new ConcurrentList<IDisposable>();

        protected readonly ModuleScope ModuleScope;

        public HostModule(HostRunnerScope scope)
        {
            Group = GetModuleGroupName(GetType());
            Name = GetModuleName(GetType());

            ModuleScope = new ModuleScope(this, scope);

            if (!scope.Context.TryGet(out IDependencyResolver resolver))
                throw new InvalidOperationException(
                    $"{nameof(HostModule)} expect a {nameof(IDependencyResolver)} in the current {nameof(HostRunnerScope)}"
                );

            var dependencyName = $"Module {Group}/{Name}";

            // the scope need to be disposed or else the type will not cleared from the GC
            var childScope = new ChildScopeContext(scope.Context);
            childScope.Register(typeof(object), this);

            Dependencies = new DependencyCollection(childScope, resolver, dependencyName);
            Dependencies.OnComplete(OnDependenciesCompleted);
            Dependencies.OnFinal(OnInit);

            Disposables.Add(childScope);
        }

        public void Dispose()
        {
            OnDispose();

            foreach (var d in Disposables)
                try
                {
                    d.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

            Disposables.Clear();
            Dependencies.Dispose();
            ModuleScope.Dispose();
        }

        IDependencyCollection IHasDependencies.Dependencies => Dependencies;

        protected virtual void OnDependenciesCompleted(IReadOnlyList<object> deps)
        {
        }

        protected internal virtual IStorage CreateDataStorage(Scope scope)
        {
            if (!scope.Context.TryGet(out IModuleCollectionStorage moduleStorage))
                throw new InvalidOperationException("Parent Storage couldn't be found!");

            var storage = (IStorage) moduleStorage;

            storage = storage.GetSubStorage(Group);
            if (!string.IsNullOrEmpty(Name))
                storage = storage.GetSubStorage(Name);
            return storage;
        }

        /// <summary>
        /// Will be called after Dependencies are completed
        /// </summary>
        protected abstract void OnInit();

        /// <summary>
        /// Track new and existing domains and invoke the callback when found
        /// </summary>
        /// <param name="onDomain">The callback</param>
        /// <typeparam name="T">Domain type</typeparam>
        /// <exception cref="InvalidOperationException">HostRunnerScope couldn't be found</exception>
        protected void TrackDomain<T>(Action<T> onDomain)
            where T : IDomain
        {
            if (!ModuleScope.Context.TryGet(out HostRunnerScope runnerScope))
                throw new InvalidOperationException("Couldn't find HostRunnerScope");

            Disposables.Add(runnerScope.TrackDomain(onDomain));
        }

        /// <summary>
        /// Load a module from group and name strings
        /// </summary>
        /// <param name="group">The group of the wanted module</param>
        /// <param name="name">The name of the module (optional, by default it will get the first module named 'Module')</param>
        /// <exception cref="InvalidOperationException">Either no World or ModuleManager was found; or the module wasn't found</exception>
        protected void LoadModule(string group, string name = "")
        {
            if (!ModuleScope.Context.TryGet(out World world))
                throw new InvalidOperationException(
                    $"No {nameof(World)} found in module scope, which is required for LoadModule(string)");

            if (!ModuleScope.Context.TryGet(out ModuleManager managerModuleSystem))
                throw new InvalidOperationException($"No {nameof(ModuleManager)} found in scope");

            var moduleEntity = managerModuleSystem.Get(group, name);
            if (!moduleEntity.IsAlive)
                throw new InvalidOperationException($"Module '{group}/{name}' not found");

            var requestEntity = world.CreateEntity();
            requestEntity.Set(new RequestLoadModule(moduleEntity.Get<HostModuleDescription>().ToPath(), moduleEntity));
        }

        /// <summary>
        /// Load a module from an existing type
        /// </summary>
        /// <param name="getModule">The method to use for deferred module creation</param>
        /// <typeparam name="TModule">The module type</typeparam>
        /// <exception cref="InvalidOperationException">The world isn't found</exception>
        protected Entity LoadModule<TModule>(Func<HostRunnerScope, TModule> getModule)
            where TModule : HostModule
        {
            return LoadModule(ModuleScope, getModule);
        }

        /// <summary>
        /// Register a module
        /// </summary>
        /// <param name="getModule">The method to use for deferred module creation</param>
        /// <typeparam name="TModule">The module type</typeparam>
        /// <returns>The module entity</returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected Entity RegisterModule<TModule>(Func<HostRunnerScope, TModule> getModule)
            where TModule : HostModule
        {
            return RegisterModule(ModuleScope, getModule);
        }

        protected virtual void OnDispose()
        {
        }

        //public static string GetModuleGroupName(Type type) => type.Assembly.GetName().Name!;
        public static string GetModuleGroupName(Type type) => type.Namespace;

        /// <summary>
        /// Get the name of a module
        /// </summary>
        /// <param name="type">The type of the module</param>
        /// <returns>Name of the module (if the name ends with Module it will be removed)</returns>
        public static string GetModuleName(Type type) => type.Name.Replace("Module", string.Empty);

        public static Entity RegisterModule<TModule>(Scope scope, Func<HostRunnerScope, TModule> getModule)
            where TModule : HostModule
        {
            if (!scope.Context.TryGet(out ModuleManager managerModuleSystem))
                throw new InvalidOperationException($"No {nameof(ModuleManager)} found in scope");

            var type = typeof(TModule);

            var moduleEntity = managerModuleSystem.GetOrCreate(GetModuleGroupName(type), GetModuleName(type));
            // Force the loading to be done via getModule (else it will use reflection)
            if (!moduleEntity.TryGet(out LoadModuleList list))
                moduleEntity.Set(list = new LoadModuleList());

            var disposable = list.Add(sc => getModule(sc));

            // automatically dispose the call of getModule when the caller has been disposed
            AssemblyLoadContext.GetLoadContext(getModule.Method.DeclaringType!.Assembly)!
                .Unloading += _ => disposable.Dispose();
            
            return moduleEntity;
        }

        public static Entity LoadModule<TModule>(Scope scope, Func<HostRunnerScope, TModule> getModule)
            where TModule : HostModule
        {
            if (!scope.Context.TryGet(out World world))
                throw new InvalidOperationException(
                    $"No {nameof(World)} found in module scope, which is required for LoadModule<T>()");

            // first register module if it doesn't exist
            var moduleEntity = RegisterModule(scope, getModule);

            // deferred, the module will not be loaded right now
            var requestEntity = world.CreateEntity();
            requestEntity.Set(new RequestLoadModule(moduleEntity.Get<HostModuleDescription>().ToPath(), moduleEntity));
            
            return moduleEntity;
        }
    }
}