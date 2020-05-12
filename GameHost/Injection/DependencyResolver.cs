using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        private ConcurrentBag<TaskCompletionSource<bool>> dependencyCompletion;

        public Task AsTask
        {
            get
            {
                if (Dependencies.Count == 0)
                    return Task.CompletedTask;
                
                var tcs = new TaskCompletionSource<bool>();
                dependencyCompletion.Add(tcs);
                return tcs.Task;
            }
        }

        public DependencyResolver(IScheduler scheduler, Context context, string source = "")
        {
            dependencyCompletion = new ConcurrentBag<TaskCompletionSource<bool>>();
            
            this.scheduler = scheduler;
            this.source = source;
        }

        public void AddDependency(DependencyBase dependency)
        {
            Dependencies.Add(dependency);
            scheduler.AddOnce(Update);
        }

        public void Add<T>([CanBeNull] IDependencyStrategy strategy = null) => AddDependency(new Dependency(strategy ?? DefaultStrategy, typeof(T)));
        public void Add<T>(ReturnByRef<T> func, IDependencyStrategy strategy = null) => AddDependency(new ReturnByRefDependency<T>(typeof(T), func, strategy ?? DefaultStrategy));

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
                var resolvedDependencies = Dependencies
                                           .Where(d => d.IsResolved && d is IResolvedObject)
                                           .Select(d => ((IResolvedObject)d).Resolved)
                                           .ToList();
                
                onComplete(resolvedDependencies);
                onComplete = null;
                Dependencies.Clear();
               // Console.WriteLine($"completed {source}");
            }
            else
            {
                var str = Dependencies.Aggregate(source, (current, dep) => current + $"\n{dep}; {dep.IsResolved}");
                //Console.WriteLine(str);
            }

            // Be sure to set the result right after onComplete has been called
            if (allResolved && dependencyCompletion.Count > 0)
            {
                foreach (var tcs in dependencyCompletion)
                    tcs.SetResult(true);
                dependencyCompletion.Clear();
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

            public override string ToString()
            {
                return $"Dependency(type={Type}, completed={Resolved != null})";
            }
        }

        public delegate ref T ReturnByRef<T>();

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
            
            public override string ToString()
            {
                return $"Dependency(type={Type}, completed={IsResolved})";
            }
        }
    }
}
