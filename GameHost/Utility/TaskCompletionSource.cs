using System.Threading.Tasks;

#if NETSTANDARD
namespace GameHost.Utility
{
	public class TaskCompletionSource : TaskCompletionSource<bool>
	{
		public void SetResult()
		{
			base.SetResult(true);
		}
	}
}
#endif