using System;
using System.Threading.Tasks;

namespace GameHost.Injection.Dependency
{
	public class TaskDependency : DependencyResolver.DependencyBase
	{
		public readonly Task Task;

		public TaskDependency(Task task)
		{
			Task = task;
		}

		public TaskDependency(Func<Task> task)
		{
			Task = task();
		}

		public override void Resolve()
		{
			IsResolved = Task.IsCompleted;
			if (Task.IsFaulted)
				ResolveException = Task.Exception;
		}

		public override string ToString()
		{
			return $"TaskDependency({Task})";
		}
	}
}