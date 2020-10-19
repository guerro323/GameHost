using System;
using System.Collections.Generic;
using GameHost.Core.Threading;
using GameHost.Injection;

namespace GameHost.Core.Ecs
{
    public abstract class AppObject : IDisposable
    {
        public bool IsDisposed { get; private set; }

        private Context context;

        /// <summary>
        /// The <see cref="Context"/> (referenced from <see cref="WorldCollection"/>) of this <see cref="AppObject"/>
        /// </summary>
        public Context Context
        {
            get => context;
            protected set
            {
                if (value == null)
                    DependencyResolver = null;

                if (!EqualityComparer<Context>.Default.Equals(context, value))
                {
                    this.context = value;
                    OnContextSet();
                }
            }
        }

        public readonly object Synchronization = new object();

        protected virtual void OnContextSet()
        {
            List<DependencyResolver.DependencyBase> inheritedDependencies = null;
            if (DependencyResolver?.Dependencies.Count > 0)
                inheritedDependencies = DependencyResolver.Dependencies;

            DependencyResolver = new DependencyResolver(new ContextBindingStrategy(Context, true).Resolve<IScheduler>(), context, $"AppObject[{GetType().Name}]")
            {
                DefaultStrategy = new DefaultAppObjectStrategy(this, context.Container.GetOrDefault<WorldCollection>())
            };

            if (inheritedDependencies != null)
                DependencyResolver.Dependencies.AddRange(inheritedDependencies);
        }

        /// <summary>
        /// The <see cref="DependencyResolver"/> of this <see cref="AppObject"/>.
        /// <remarks>
        /// The first initialization of the resolver is using the strategy <see cref="DefaultAppObjectStrategy"/>
        /// </remarks>
        /// </summary>
        public DependencyResolver DependencyResolver { get; protected set; }

        protected List<IDisposable> ReferencedDisposables;

        public AppObject(Context context)
        {
            ReferencedDisposables = new List<IDisposable>(8);
            Context               = context;
        }

        /// <summary>
        /// Add an object with <see cref="IDisposable"/>> that will be disposed when <see cref="Dispose"/> is called on this system.
        /// </summary>
        /// <param name="disposable"></param>
        public void AddDisposable(IDisposable disposable) => ReferencedDisposables.Add(disposable);

        public virtual void Dispose()
        {
            lock (Synchronization)
            {
                if (IsDisposed)
                    Console.WriteLine("Disposing an already disposed AppObject " + GetType());

                Console.WriteLine("Disposing: " + GetType());
                foreach (var d in ReferencedDisposables)
                {
                    try
                    {
                        d.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                ReferencedDisposables.Clear();
                IsDisposed = true;
            }
        }
    }
}