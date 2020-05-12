using System;
using System.Collections.Generic;
using System.Threading;

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

        public Scheduler(Thread targetThread = null)
        {
            scheduleThread        = targetThread ?? Thread.CurrentThread;
            synchronizationObject = new object();
            scheduledValueTasks   = new Queue<ScheduledValueTask>();
        }

        public bool IsOnSameThread() => scheduleThread == Thread.CurrentThread;

        public void Run()
        {
            lock (synchronizationObject)
            {
                var taskLength = scheduledValueTasks.Count;
                var i          = 0;
                while (i++ < taskLength && scheduledValueTasks.TryDequeue(out var task))
                {
                    try
                    {
                        task.Invoke();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }
            }
        }

        public void Add(Action action)
        {
            lock (synchronizationObject)
            {
                scheduledValueTasks.Enqueue(new ScheduledValueTask {Action = action});
            }
        }

        public void AddOnce(Action action)
        {
            lock (synchronizationObject)
            {
                foreach (var task in scheduledValueTasks)
                    if (task.Action == action)
                        return;

                scheduledValueTasks.Enqueue(new ScheduledValueTask {Action = action});
            }
        }

        private struct ScheduledValueTask
        {
            public Action Action;

            public void Invoke() => Action.Invoke();
        }
    }
}
