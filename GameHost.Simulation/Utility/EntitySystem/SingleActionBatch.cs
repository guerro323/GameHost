using System;

namespace StormiumTeam.GameBase.Utility.Misc.EntitySystem
{
	public class SingleActionBatch<T> : IBatch
	{
		public Action<T> Action;

		public SingleActionBatch(Action<T> action, T data = default)
		{
			Action    = action;
			this.data = data;
		}

		private T data;
		public void PrepareData(T data)
		{
			this.data = data;
		}

		public int  PrepareBatch(int taskCount)
		{
			return 1;
		}

		public void Execute(int      index, int maxUseIndex, int task, int taskCount)
		{
			Console.WriteLine(task);
			Action(data);
		}
	}
}