using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DryIoc;
using GameHost.Core.Applications;
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
    public abstract class AppSystem : AppObject, IDisposable, IInitSystem, IUpdateSystem
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
            ApplicationHostBase currentApplication;
            try
            {
                currentApplication = new ContextBindingStrategy(Context, true).Resolve<ApplicationHostBase>();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return true;
            }

            var attr = GetType().GetCustomAttribute<RestrictToApplicationAttribute>();
            if (attr == null || attr.IsValid(currentApplication.GetType()))
                return true;
            return false;
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
            
            DependencyResolver                 = new DependencyResolver(Context.Container.Resolve<IScheduler>(), Context, $"Thread({Thread.CurrentThread.Name}) System[{GetType().Name}]");
            DependencyResolver.DefaultStrategy = new DefaultAppObjectStrategy(this, World);
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
            return Enabled && DependencyResolver.Dependencies.Count == 0;
        }

        WorldCollection IWorldSystem.WorldCollection => World;
    }
}
