using System;
using GameHost.Core.Ecs;

[assembly:AllowAppSystemResolving]

namespace GameHost
{
	public class Program
	{
		public static void Main(string[] args)
		{
			throw new Exception("GameHost can not yet be run directly.");
		}
	}
}