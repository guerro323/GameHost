using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GameHost.Utility
{
	public class SameThreadTaskScheduler : TaskScheduler
	{
		private List<Task> tasks = new List<Task>();
		private Thread     currentThread;

		public SameThreadTaskScheduler()
		{
			currentThread = Thread.CurrentThread;
		}
			
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return tasks;
		}

		protected override void QueueTask(Task task)
		{
			tasks.Add(task);
		}

		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return Thread.CurrentThread == currentThread && TryExecuteTask(task);
		}

		public void Execute()
		{
			var count = tasks.Count;
			while (count-->0)
			{
				TryExecuteTask(tasks[0]);
				tasks.RemoveAt(0);
			}
		}
	}

	public static class TaskRunUtility
	{
		[Obsolete("use extension method")]
		public static Task StartUnwrap(Func<CancellationToken, Task> taskCreator, TaskScheduler taskScheduler, CancellationToken cancellationToken)
		{
			return Task.Factory
			           .StartNew(() => taskCreator(cancellationToken), cancellationToken, TaskCreationOptions.AttachedToParent, taskScheduler)
			           .Unwrap();
		}
		
		public static Task StartUnwrap(this TaskScheduler taskScheduler, Func<CancellationToken, Task> taskCreator, CancellationToken cancellationToken)
		{
			return Task.Factory
			           .StartNew(() => taskCreator(cancellationToken), cancellationToken, TaskCreationOptions.AttachedToParent, taskScheduler)
			           .Unwrap();
		}

		public static Task StartUnwrap(this TaskScheduler taskScheduler, Func<Task> taskCreator)
		{
			return Task.Factory
			           .StartNew(taskCreator, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler)
			           .Unwrap();
		}
	}
}