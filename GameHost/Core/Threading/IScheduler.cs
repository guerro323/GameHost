using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GameHost.Core.Threading
{
    public interface IScheduler
    {
        bool IsOnSameThread();
        void Run();
        void Add(Action     action);
        void AddOnce(Action action);
    }

    public class Scheduler : IScheduler
    {
        private Thread scheduleThread;

        private object synchronizationObject;

        private Queue<ScheduledValueTask> scheduledValueTasks;
        private List<ScheduledValueTask> nextRunningTasks;

        private Action debugLastTask;
        private string debugStacktrace;

        private SpinLock spinLock;
        
        public Scheduler(Thread targetThread = null)
        {
            scheduleThread        = targetThread ?? Thread.CurrentThread;
            synchronizationObject = new object();
            scheduledValueTasks   = new Queue<ScheduledValueTask>();
            nextRunningTasks = new List<ScheduledValueTask>();
            
            spinLock = new SpinLock(true);
        }

        public bool IsOnSameThread() => scheduleThread == Thread.CurrentThread;

        public void Run()
        {
            var lockTaken = false;
            spinLock.TryEnter(TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
            {
                try
                {
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
                    debugLastTask = task.Action;
                    task.Invoke();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
        }

        public void Add(Action action)
        {
            var lockTaken = false;
            spinLock.TryEnter(TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
            {
                scheduledValueTasks.Enqueue(new ScheduledValueTask {Action = action});
                spinLock.Exit(true);
            }
            else
            {
                Console.WriteLine($"Couldn't enter scheduler! Action={action.Method} LastTask={debugLastTask.Method}");
            }
        }

        public void AddOnce(Action action)
        {
            var lockTaken = false;
            spinLock.TryEnter(TimeSpan.FromSeconds(1), ref lockTaken);
            if (lockTaken)
            {
                if (scheduledValueTasks.Any(task => task.Action == action))
                {
                    spinLock.Exit(true);
                    return;
                }

                scheduledValueTasks.Enqueue(new ScheduledValueTask {Action = action});
                spinLock.Exit(true);
            }
            else
            {
                Console.WriteLine($"Couldn't enter scheduler! Action={action.Method} LastTask={debugLastTask.Method}");
            }
        }

        private struct ScheduledValueTask
        {
            public Action Action;

            public void Invoke() => Action.Invoke();
        }
    }
}
