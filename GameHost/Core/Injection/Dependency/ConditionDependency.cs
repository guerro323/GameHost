using System;
using System.Threading.Tasks;

namespace GameHost.Injection.Dependency
{
	public class ConditionDependency : DependencyResolver.DependencyBase
	{
		public readonly Func<bool> Task;
		
		public ConditionDependency(Func<bool> task)
		{
			Task = task;
		}

		public override void Resolve()
		{
			try
			{
				IsResolved = Task();
			}
			catch (Exception ex)
			{
				ResolveException = ex;
			}
		}

		public override string ToString()
		{
			return $"ConditionDependency({Task})";
		}
	}
}