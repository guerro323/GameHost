using System;
using System.Collections.Generic;
using System.Threading;

namespace GameHost.Core.Threading
{
    public interface IScheduler
    {
        bool IsOnSameThread();
        void Run();
        void Schedule(Action action);
    }

    public class Scheduler : IScheduler
    {
        private Thread scheduleThread;

        private object synchronizationObject;
        private Queue<ScheduledTask> scheduledTasks;
        
        public Scheduler(Thread targetThread = null)
        {
            scheduleThread = targetThread ?? Thread.CurrentThread;
            synchronizationObject = new object();
            scheduledTasks = new Queue<ScheduledTask>();
        }

        public bool IsOnSameThread() => scheduleThread == Thread.CurrentThread;
        public void Run()
        {
            lock (scheduledTasks)
            {
                while (scheduledTasks.TryDequeue(out var task))
                    task.Invoke();
            }
        }

        public void Schedule(Action action)
        {
            lock (synchronizationObject)
                scheduledTasks.Enqueue(new ScheduledTask {Action = action});
        }

        private class ScheduledTask
        {
            public Action Action;

            public void Invoke() => Action.Invoke();
        }
    }
}
