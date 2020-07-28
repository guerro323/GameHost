using System;
using System.Collections.Generic;
using System.Threading;
using DryIoc;
using GameHost.Applications;
using GameHost.Core.Ecs.Passes;
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
    public abstract class AppSystem : AppObject, IDisposable, IInitializePass, IUpdatePass
    {
        /// <summary>
        /// Is this <see cref="AppSystem"/> enabled?
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// The <see cref="WorldCollection"/> of this <see cref="AppSystem"/>.
        /// </summary>
        public WorldCollection World { get; private set; }

        public virtual bool CanBeCreated()
        {
            return true;
        }

        public AppSystem(WorldCollection collection) : base(null)
        {
            World   = collection;
            Context = collection.Ctx;

            if (!CanBeCreated())
                throw new InvalidOperationException("This system was constructed but shouldn't have been.");
        }

        protected override void OnContextSet()
        {
            List<DependencyResolver.DependencyBase> inheritedDependencies = null;
            if (DependencyResolver?.Dependencies.Count > 0)
                inheritedDependencies = DependencyResolver.Dependencies;

            IApplication currentApp;

            var category = string.Empty;
            if ((currentApp = new ContextBindingStrategy(Context, false).Resolve<IApplication>()) != null)
            {
                category = $"App({currentApp.GetType().Name})";
            }
            else
            {
                category = $"Thread({Thread.CurrentThread.Name})";
            }

            DependencyResolver = new DependencyResolver(new ContextBindingStrategy(Context, true).Resolve<IScheduler>(), Context, $"{category} System[{GetType().Name}]")
            {
                DefaultStrategy = new DefaultAppObjectStrategy(this, World)
            };
            DependencyResolver.OnComplete(OnDependenciesResolved);

            if (inheritedDependencies != null)
                DependencyResolver.Dependencies.AddRange(inheritedDependencies);
        }

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
        public override void Dispose()
        {
            base.Dispose();
        }

        // Interfaces implementation
        // .........................

        void IInitializePass.OnInit()
        {
            OnInit();
        }

        void IUpdatePass.OnUpdate()
        {
            OnUpdate();
        }

        /// <summary>
        /// Return a boolean that indicate if this system can invoke <see cref="OnUpdate"/>
        /// </summary>
        /// <returns></returns>
        public virtual bool CanUpdate()
        {
            return Enabled && DependencyResolver.Dependencies.Count == 0;
        }

        WorldCollection IWorldSystem.WorldCollection => World;
    }
}