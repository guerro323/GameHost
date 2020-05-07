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

        public static int GetWorkerCount(float parallelismPercentage)
        {
            return Math.Clamp((int)(ProcessorCount * parallelismPercentage), 1, ProcessorCount);
        }
        
        public static int GetWorkerCount(double parallelismPercentage)
        {
            return Math.Clamp((int)(ProcessorCount * parallelismPercentage), 1, ProcessorCount);
        }
    }
}
