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
        public bool Enabled { get; set; } = true;

        public Context         Context { get; private set; }
        public WorldCollection World   { get; private set; }
        public DependencyResolver DependencyResolver { get; private set; }

        private IEnumerable<object> callResolved;
        private List<IDisposable> disposables = new List<IDisposable>();

        public void AddDisposable(IDisposable disposable) => disposables.Add(disposable);

        protected virtual void OnInit()
        {

        }

        protected virtual void OnUpdate()
        {
        
        }

        protected virtual void OnDependenciesResolved(IEnumerable<object> dependencies)
        {
            
        }

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
                    DefaultStrategy = new ContextBindingStrategy(Context, true)
                };
                DependencyResolver.OnComplete(enumerable => callResolved = enumerable);
            }
        }
    }
}
