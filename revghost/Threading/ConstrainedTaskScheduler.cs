using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace revghost.Threading;

/// <summary>
/// Constraint the task scheduler to run on the thread that was invoked on.
/// </summary>
/// <remarks>
/// This TaskScheduler can't create threads, nor navigate to any thread that a task can create (except if forced)
/// </remarks>
public class ConstrainedTaskScheduler : TaskScheduler
{
    private List<Task> tasks = new List<Task>();
    private Thread currentThread;

    public override int MaximumConcurrencyLevel => 1;

    public ConstrainedTaskScheduler()
    {
        currentThread = Thread.CurrentThread;
    }

    protected override IEnumerable<Task> GetScheduledTasks()
    {
        return ArraySegment<Task>.Empty;
    }

    protected override void QueueTask(Task task)
    {
        lock (tasks)
        {
            tasks.Add(task);
        }
    }

    protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
    {
        if (Thread.CurrentThread == currentThread && TryExecuteTask(task))
            return true;

        // If the task wasn't started, this mean it wanted to be on another thread, so let's queue it.
        if (Thread.CurrentThread != currentThread)
            QueueTask(task);

        // else it's finished, so no need to re-execute it
        return false;

    }

    private List<Task> runQueue = new();

    public void Execute()
    {
        // make sure that the current thread is reset each time we do Execute()
        currentThread = Thread.CurrentThread;

        lock (tasks)
        {
            runQueue.Clear();
            foreach (var task in tasks)
            {
                if (task is null)
                    throw new NullReferenceException("null task");
                runQueue.Add(task);
            }

            tasks.Clear();
        }

        foreach (var task in runQueue)
        {
            TryExecuteTask(task);
        }
    }
}