using System;
using System.Collections.Concurrent;

namespace GameHost.Core.Threading
{
    public class WorkerCollection
    {
        public readonly ConcurrentBag<Worker> Workers;

        public WorkerCollection()
        {
            Workers = new ConcurrentBag<Worker>();
        }

        public ReadOnlySpan<Worker> GetWorkersByName(string name)
        {
            var array = new Worker[Workers.Count];
            var max   = 0;
            foreach (var worker in Workers)
            {
                if (Worker.GetName(worker) == name)
                    array[max++] = worker;
            }

            return new ReadOnlySpan<Worker>(array, 0, max);
        }
    }
}
