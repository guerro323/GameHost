﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GameHost.Core.Threading
{
    public struct SchedulingParameters
    {
        public bool Once;

        public static readonly SchedulingParameters AsOnce = new SchedulingParameters {Once = true};
    }

    public struct SchedulingParametersWithArgs
    {
        public bool OnceWithMethod;
        public bool OnceWithMethodAndArgs;

        public static readonly SchedulingParametersWithArgs AsOnce         = new SchedulingParametersWithArgs {OnceWithMethod        = true};
        public static readonly SchedulingParametersWithArgs AsOnceWithArgs = new SchedulingParametersWithArgs {OnceWithMethodAndArgs = true};
    }

    public interface IScheduler
    {
        void Run();

        void Schedule(Action       action, in SchedulingParameters parameters);
        void Schedule<T>(Action<T> action, T                       args, in SchedulingParametersWithArgs parameters);
    }

    public class Scheduler : IScheduler
    {
        private class __NullArg
        {
        }

        private abstract class Collection
        {
            public abstract Delegate DequeueAndInvoke();
            public abstract bool     Contains(Delegate   del);
            public abstract void     TakeFrom(Collection other);
        }

        private class NoArgCollection : Collection
        {
            public Queue<Action> Scheduled = new Queue<Action>(8);

            public ScheduledValueTask Enqueue(Action action)
            {
                Scheduled.Enqueue(action);
                return new ScheduledValueTask {Type = typeof(__NullArg)};
            }

            public override Delegate DequeueAndInvoke()
            {
                if (Scheduled.TryDequeue(out var action))
                {
                    action();
                    return action;
                }

                return null;
            }

            public override bool Contains(Delegate del)
            {
                return Scheduled.Contains(del);
            }

            public override void TakeFrom(Collection other)
            {
                var same = (NoArgCollection) other;
                while (same.Scheduled.TryDequeue(out var action))
                    Scheduled.Enqueue(action);
            }
        }

        private class Collection<T> : Collection
        {
            public Queue<(Action<T> action, T args)> Scheduled = new Queue<(Action<T>, T)>(4);

            public ScheduledValueTask Enqueue(Action<T> action, in T args)
            {
                Scheduled.Enqueue((action, args));
                return new ScheduledValueTask {Type = typeof(T)};
            }

            public override Delegate DequeueAndInvoke()
            {
                if (Scheduled.TryDequeue(out var pool))
                {
                    pool.action(pool.args);
                    return pool.action;
                }

                return null;
            }

            public override bool Contains(Delegate del)
            {
                foreach (var scheduled in Scheduled)
                    if (scheduled.action == (Action<T>) del)
                        return true;
                return false;
            }

            public override void TakeFrom(Collection other)
            {
                var same = (Collection<T>) other;
                while (same.Scheduled.TryDequeue(out var action))
                    Scheduled.Enqueue(action);
            }
        }

        /// <summary>
        /// If true, continue the scheduler Run() on exception.
        /// </summary>
        public Func<Exception, bool> OnExceptionFound;

        private Queue<ScheduledValueTask> scheduledValueTasks;
        private List<ScheduledValueTask>  nextRunningTasks;

        private Dictionary<Type, Collection> scheduledCollectionTypeMap;
        private Dictionary<Type, Collection> runningCollectionTypeMap;

        private Delegate debugLastTask;

        private SpinLock spinLock;

        public Scheduler(Func<Exception, bool> onExceptionFound = null)
        {
            this.OnExceptionFound = onExceptionFound ?? (ex =>
            {
                Console.WriteLine(ex);
                return false;
            });

            scheduledValueTasks = new Queue<ScheduledValueTask>();
            nextRunningTasks    = new List<ScheduledValueTask>();

            scheduledCollectionTypeMap = new Dictionary<Type, Collection>()
            {
                {typeof(__NullArg), new NoArgCollection()}
            };
            runningCollectionTypeMap = new Dictionary<Type, Collection>()
            {
                {typeof(__NullArg), new NoArgCollection()}
            };

            spinLock = new SpinLock(true);
        }

        public void Run()
        {
            var lockTaken = false;
            spinLock.TryEnter(TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
            {
                try
                {
                    foreach (var (type, scheduled) in scheduledCollectionTypeMap)
                    {
                        var running = runningCollectionTypeMap[type];
                        running.TakeFrom(scheduled);
                    }

                    nextRunningTasks.Clear();
                    while (scheduledValueTasks.TryDequeue(out var task))
                        nextRunningTasks.Add(task);
                }
                finally
                {
                    spinLock.Exit(true);
                }
            }
            else
                throw new InvalidOperationException("Couldn't Sync!");

            foreach (var task in nextRunningTasks)
            {
                try
                {
                    debugLastTask = task.Invoke(runningCollectionTypeMap);
                }
                catch (Exception ex)
                {
                    if (!OnExceptionFound(ex))
                        return;
                }
            }
        }

        public void Schedule(Action action, in SchedulingParameters parameters)
        {
            var lockTaken = false;
            spinLock.TryEnter(TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
            {
                if (parameters.Once && scheduledCollectionTypeMap[typeof(__NullArg)].Contains(action))
                {
                    spinLock.Exit(true);
                    return;
                }
                
                scheduledValueTasks.Enqueue(((NoArgCollection) scheduledCollectionTypeMap[typeof(__NullArg)]).Enqueue(action));
                spinLock.Exit(true);
            }
            else
            {
                Console.WriteLine($"Couldn't enter scheduler! Action={action.Method} LastTask={debugLastTask.Method}");
            }
        }

        public void Schedule<T>(Action<T> action, T args, in SchedulingParametersWithArgs parameters)
        {
            var lockTaken = false;
            spinLock.TryEnter(TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
            {
                if (!scheduledCollectionTypeMap.TryGetValue(typeof(T), out var collection))
                {
                    collection = new Collection<T>();

                    scheduledCollectionTypeMap[typeof(T)] = collection;
                    runningCollectionTypeMap[typeof(T)]   = new Collection<T>();
                }

                if (parameters.OnceWithMethod && collection.Contains(action))
                {
                    spinLock.Exit(true);
                    return;
                }

                if (parameters.OnceWithMethodAndArgs && ((Collection<T>) collection).Scheduled
                                                                                     .Any(t => t.action == action && t.args.Equals(args)))
                {
                    spinLock.Exit(true);
                    return;
                }
                
                scheduledValueTasks.Enqueue(((Collection<T>) collection).Enqueue(action, args));
                spinLock.Exit(true);
            }
            else
            {
                Console.WriteLine($"Couldn't enter scheduler! Action={action.Method} LastTask={debugLastTask.Method}");
            }
        }

        private struct ScheduledValueTask
        {
            public Type Type;

            public Delegate Invoke(IReadOnlyDictionary<Type, Collection> map) => map[Type].DequeueAndInvoke();
        }
    }
}