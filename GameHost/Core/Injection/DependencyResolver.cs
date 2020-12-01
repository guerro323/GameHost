using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            scheduler.Schedule(Update, default);
        }

        public void Add<T>([CanBeNull] IDependencyStrategy strategy = null) => AddDependency(new Dependency(strategy ?? DefaultStrategy, typeof(T)));
        public void Add<T>(ReturnByRef<T> func, IDependencyStrategy strategy = null) => AddDependency(new ReturnByRefDependency<T>(typeof(T), func, strategy ?? DefaultStrategy));

        private Action<IEnumerable<object>> onComplete;

        public void OnComplete(Action<IEnumerable<object>> action)
        {
            onComplete = action;
        }

        private int unresolvedFrames;
        
        public bool TryComplete()
        {
            if (Dependencies.Count == 0)
                return false;
            
            Update();
            return Dependencies.Count == 0;
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
                    Console.WriteLine($"Resolving on item '{dep}' for '{source}' dependencies has failed!\n{ex}");
                }

                if (!dep.IsResolved)
                    allResolved = false;
            }

            if (allResolved && Dependencies.Count > 0)
            {
                var resolvedDependencies = Dependencies
                                           .Where(d => d.IsResolved && d is IResolvedObject)
                                           .Select(d => ((IResolvedObject) d).Resolved)
                                           .ToList();

                Dependencies.Clear();
                if (onComplete != null)
                {
                    onComplete(resolvedDependencies);
                    onComplete = null;
                }

                // Here we go again!
                if (Dependencies.Count > 0)
                    allResolved = false;

                //Console.WriteLine($"completed {source}");
                unresolvedFrames = 0;
            }
            else if (unresolvedFrames++ > 100)
            {
                unresolvedFrames = 0;
            
                var str = Dependencies.Aggregate(source, (current, dep) => current + $"\n\t{dep}; {dep.IsResolved}");
                Console.WriteLine(str);
            }

            // Be sure to set the result right after onComplete has been called (in case new deps has been added)
            if (allResolved && dependencyCompletion.Count > 0)
            {
                foreach (var tcs in dependencyCompletion)
                    tcs.SetResult(true);
                dependencyCompletion.Clear();
            }

            if (!allResolved)
                scheduler.Schedule(Update, default);
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

            private readonly Func<object> resolver;
            public Dependency(IDependencyStrategy strategy, Type type)
            {
                Strategy = strategy;
                Type     = type;

                resolver = Strategy.GetResolver(type);
            }

            public override void Resolve()
            {
                IsResolved = (Resolved = resolver()) != null;
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

            private readonly Func<object> resolver;
            public ReturnByRefDependency(Type type, ReturnByRef<T> fun, IDependencyStrategy strategy)
            {
                Type     = type;
                Function = fun;
                Strategy = strategy;
                
                resolver = strategy.GetResolver(type);
            }

            public override void Resolve()
            {
                Resolved = resolver();
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
                return $"Dependency(type={Type}, strategy={Strategy}, completed={IsResolved})";
            }
        }
    }
}
