using System;

namespace GameHost.Simulation.Utility.EntitySystem
{
    public class SingleActionBatch<T> : IBatch
    {
        public Action<T> Action;

        private T data;

        public SingleActionBatch(Action<T> action, T data = default)
        {
            Action = action;
            this.data = data;
        }

        public int PrepareBatch(int taskCount)
        {
            return 1;
        }

        public void Execute(int index, int maxUseIndex, int task, int taskCount)
        {
            Console.WriteLine(task);
            Action(data);
        }

        public void PrepareData(T data)
        {
            this.data = data;
        }
    }
}