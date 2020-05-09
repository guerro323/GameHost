using System;
using System.Collections.Generic;
using System.Linq;
using GameHost.Core.IO;
using GameHost.Core.Threading;
using JetBrains.Annotations;

namespace GameHost.Injection
{
    public class DependencyResolver
    {
        public List<DependencyBase> Dependencies = new List<DependencyBase>();
        public IDependencyStrategy  DefaultStrategy;

        private readonly IScheduler scheduler;
        private readonly string source;

        public DependencyResolver(IScheduler scheduler, Context context, string source = "")
        {
            this.scheduler = scheduler;
            this.source = source;
        }

        public void Add<T>([CanBeNull] IDependencyStrategy strategy = null)
        {
            Dependencies.Add(new Dependency(strategy ?? DefaultStrategy, typeof(T)));
            scheduler.AddOnce(Update);
        }

        private Action<IEnumerable<object>> onComplete;

        public void OnComplete(Action<IEnumerable<object>> action)
        {
            onComplete = action;
        }

        private void Update()
        {
            var allResolved = true;
            for (var i = 0; i != Dependencies.Count; i++)
            {
                var dep = Dependencies[i];
                if (dep.IsResolved)
                    continue;

                try
                {
                    dep.Resolve();
                }
                catch (Exception ex)
                {
                    dep.ResolveException = ex;
                    Console.WriteLine(ex);
                }

                if (!dep.IsResolved)
                    allResolved = false;
            }
            
            if (allResolved && onComplete != null)
            {
                onComplete(Dependencies.Where(d => d.IsResolved && d is IResolvedObject).Select(d => ((IResolvedObject)d).Resolved));
                onComplete = null;
                Dependencies.Clear();
            }

            if (!allResolved)
                scheduler.AddOnce(Update);
        }

        public interface IResolvedObject
        {
            object Resolved { get; }
        }

        public abstract class DependencyBase
        {
            public IDependencyStrategy Strategy;
            public bool                IsResolved { get; protected set; }

            public Exception ResolveException;

            public abstract void Resolve();
        }

        public class Dependency : DependencyBase, IResolvedObject
        {
            public Type Type;

            public Dependency(IDependencyStrategy strategy, Type type)
            {
                Strategy = strategy;
                Type     = type;
            }

            public override void Resolve()
            {
                IsResolved = (Resolved = Strategy.Resolve(Type)) != null;
            }

            public object Resolved { get; private set; }
        }

        public delegate ref T ReturnByRef<T>();

        public void Add<T>(ReturnByRef<T> func, IDependencyStrategy strategy = null)
        {
            Dependencies.Add(new ReturnByRefDependency<T>(typeof(T), func, strategy ?? DefaultStrategy));
            scheduler.AddOnce(Update);
        }

        public class ReturnByRefDependency<T> : DependencyBase, IResolvedObject
        {
            public Type           Type;
            public ReturnByRef<T> Function;

            public object Resolved { get; private set; }

            public ReturnByRefDependency(Type type, ReturnByRef<T> fun, IDependencyStrategy strategy)
            {
                Type     = type;
                Function = fun;
                Strategy = strategy;
            }

            public override void Resolve()
            {
                Resolved = Strategy.Resolve(Type);
                if (Resolved != null)
                {
                    // The ref object will be replaced by the resolved.
                    Function() = (T)Resolved;
                    IsResolved = true;
                    return;
                }
                
                IsResolved = false;
            }
        }
    }
}
