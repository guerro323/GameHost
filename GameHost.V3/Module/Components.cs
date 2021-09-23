using System;
using System.Collections;
using System.Collections.Generic;
using DefaultEcs;

namespace GameHost.V3.Module
{
    // delegates can be components, in this case LoadModule is one (maybe it would be better to have it encapsulated into a struct?)
    
    public delegate HostModule LoadModule(HostRunnerScope scope);

    public class LoadModuleList : IEnumerable<LoadModule>
    {
        public readonly List<LoadModule> List = new();

        private class Disposable : IDisposable
        {
            private readonly List<LoadModule> _loadModules;
            private readonly LoadModule _action;

            public Disposable(List<LoadModule> modules, LoadModule action)
            {
                _loadModules = modules;
                _action = action;

                _loadModules.Add(_action);
            }

            public void Dispose()
            {
                _loadModules.Remove(_action);
            }
        }

        public IDisposable Add(LoadModule action)
        {
            return new Disposable(List, action);
        }

        public IEnumerator<LoadModule> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return List.GetEnumerator();
        }
    }

    /// <summary>
    /// Check for modules on the next update event.
    /// </summary>
    /// <code>
    /// Default.EcsWorld world;
    /// world.CreateEntity()
    ///      .Set(new RefreshModuleList());
    /// </code>
    public struct RefreshModuleList
    {
    }

    /// <summary>
    /// Registered available module, the entity that have this component can be used as unloading/loading
    /// </summary>
    public struct RegisteredModule
    {
    }

    /// <summary>
    /// State of a module
    /// </summary>
    public enum ModuleState
    {
        /// <summary>
        /// No state, this can mean the module was either unloaded or never loaded.
        /// </summary>
        None,
        /// <summary>
        /// The module is being loaded
        /// </summary>
        /// <remarks>
        /// IsLoading is applied once a module start being loaded, and is finished when all dependencies of this module are completed
        /// </remarks>
        IsLoading,
        /// <summary>
        /// The module is loaded
        /// </summary>
        Loaded,
        /// <summary>
        /// The module is currently being unloaded
        /// </summary>
        Unloading,
        /// <summary>
        /// If a module couldn't be unloaded (eg: remaining trace in the GC or leaks) then it will be in a zombie state
        /// </summary>
        Zombie
    }
    
    /// <summary>
    /// Request to load a module, attach it to a newly created entity.
    /// </summary>
    /// <remarks>
    /// <see cref="Name"/> is optional, it's mostly used for debug purpose.
    /// <see cref="Module"/> must be an entity with a <see cref="RegisteredModule"/> component.
    /// </remarks>
    /// <code>
    /// DefaultEcs.World world;
    /// world.CreateEntity()
    ///      .Set(new RequestLoadModule("debug name", moduleEntity);
    /// </code>
    public readonly struct RequestLoadModule
    {
        public readonly string Name;
        public readonly Entity Module;

        public RequestLoadModule(string name, Entity module)
        {
            Name = name;
            Module = module;
        }
    }
    
    public readonly struct RequestReloadModule
    {
        public readonly string Name;
        public readonly Entity Module;

        public RequestReloadModule(string name, Entity module)
        {
            Name = name;
            Module = module;
        }
    }

    public struct RequestUnloadModule
    {
        public Entity Module;
    }
}