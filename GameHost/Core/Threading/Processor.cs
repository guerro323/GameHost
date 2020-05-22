using System;

namespace GameHost.Core.Threading
{
    public static class Processor
    {
        private static readonly int ProcessorCount;

        static Processor()
        {
            ProcessorCount = Environment.ProcessorCount;
        }

        public static int GetWorkerCount(double parallelismPercentage)
        {
            // right now we can't have parallel operations until we have an intelligent worker counter 
            return 1;
            return Math.Clamp((int)(ProcessorCount * parallelismPercentage), 1, ProcessorCount);
        }
    }
}
