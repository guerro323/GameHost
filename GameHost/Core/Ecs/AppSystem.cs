using System;
using System.Collections.Generic;
using System.Threading;
using DryIoc;
using GameHost.Core.Threading;
using GameHost.Injection;
using JetBrains.Annotations;

namespace GameHost.Core.Ecs
{
    [AttributeUsage(AttributeTargets.Class)]
    [UsedImplicitly] // would it possible to 'remove' it when DontInject attribute is present?
    public class InjectSystemToWorldAttribute : Attribute
    {
    }

    /// <summary>
    /// Adding this attribute to a system will make it not inject into a world.
    /// It will ignore parents <see cref="InjectSystemToWorldAttribute"/>.
    /// Children that use <see cref="InjectSystemToWorldAttribute"/> will ignore this attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    [Obsolete("This attribute is not yet implemented.")]
    public class DontInjectSystemToWorldAttribute : Attribute
    {
    }

    /// <summary>
    /// Represent an application system that will automatically be injected in worlds.
    /// </summary>
    [InjectSystemToWorld]
    public abstract class AppSystem : IDisposable, IInitSystem, IUpdateSystem
    {
        /// <summary>
        /// Is this <see cref="AppSystem"/> enabled?
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The <see cref="Context"/> (referenced from <see cref="WorldCollection"/>) of this <see cref="AppSystem"/>
        /// </summary>
        public Context         Context { get; private set; }
        /// <summary>
        /// The <see cref="WorldCollection"/> of this <see cref="AppSystem"/>.
        /// </summary>
        public WorldCollection World   { get; private set; }
        /// <summary>
        /// The <see cref="DependencyResolver"/> of this <see cref="AppSystem"/>.
        /// <remarks>
        /// The first initialization of the resolver is using the strategy <see cref="DefaultAppSystemStrategy"/>
        /// </remarks>
        /// </summary>
        public DependencyResolver DependencyResolver { get; private set; }

        private IEnumerable<object> callResolved;
        private List<IDisposable> disposables = new List<IDisposable>();

        /// <summary>
        /// Add an object with <see cref="IDisposable"/>> that will be disposed when <see cref="Dispose"/> is called on this system.
        /// </summary>
        /// <param name="disposable"></param>
        public void AddDisposable(IDisposable disposable) => disposables.Add(disposable);

        protected virtual void OnInit()
        {

        }

        protected virtual void OnUpdate()
        {
        
        }

        /// <summary>
        /// This function is called when all initial dependencies are resolved.
        /// <remarks>
        /// Called right before <see cref="OnUpdate"/>
        /// </remarks>
        /// </summary>
        /// <param name="dependencies"></param>
        protected virtual void OnDependenciesResolved(IEnumerable<object> dependencies)
        {

        }

        /// <summary>
        /// Disposing the system and its internal resources.
        /// </summary>
        public virtual void Dispose()
        {
            foreach (var disposable in disposables)
                disposable?.Dispose();
            disposables.Clear();
        }

        // Interfaces implementation
        // .........................

        void IInitSystem.OnInit()
        {
            OnInit();
        }

        void IUpdateSystem.OnUpdate()
        {
            OnUpdate();
        }

        /// <summary>
        /// Return a boolean that indicate if this system can invoke <see cref="OnUpdate"/>
        /// </summary>
        /// <returns></returns>
        public virtual bool CanUpdate()
        {
            if (callResolved != null)
            {
                OnDependenciesResolved(callResolved);
                callResolved = null;
            }
            return Enabled && DependencyResolver.Dependencies.Count == 0;
        }

        WorldCollection IWorldSystem.WorldCollection
        {
            get
            {
                return World;
            }
            set
            {
                World   = value;
                Context = value.Ctx;

                DependencyResolver = new DependencyResolver(Context.Container.Resolve<IScheduler>(), Context, $"Thread({Thread.CurrentThread.Name}) System[{GetType().Name}]")
                {
                    DefaultStrategy = new DefaultAppSystemStrategy(this, World)
                };
                DependencyResolver.OnComplete(enumerable => callResolved = enumerable);
            }
        }
    }
}
