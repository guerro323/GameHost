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

		public override int MaximumConcurrencyLevel => 1;

		public SameThreadTaskScheduler()
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

		public void Execute()
		{
			// make sure that the current thread is reset each time we do Execute()
			currentThread = Thread.CurrentThread;

			lock (tasks)
			{
				var count = tasks.Count;
				for (var i = 0; i < count; i++)
				{
					if (tasks[0] is null)
						throw new NullReferenceException("null task");

					TryExecuteTask(tasks[0]);
					tasks.RemoveAt(0);
				}
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
		
		public static Task<T> StartUnwrap<T>(this TaskScheduler taskScheduler, Func<CancellationToken, Task<T>> taskCreator, CancellationToken cancellationToken)
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
		
		public static Task<T> StartUnwrap<T>(this TaskScheduler taskScheduler, Func<Task<T>> taskCreator)
		{
			return Task.Factory
			           .StartNew(taskCreator, CancellationToken.None, TaskCreationOptions.AttachedToParent, taskScheduler)
			           .Unwrap();
		}
	}
}