using System;
using System.Threading;
using GameHost.Core.Ecs;
using GameHost.Game;
using HelloWorldTemplate.Data;

// This will allow GameBootstrap to resolve systems automatically for us on this assembly
[assembly: AllowAppSystemResolving]

namespace HelloWorldTemplate
{
	class Program
	{
		static void Main(string[] args)
		{
			using var game = new GameBootstrap();
			game.GameEntity.Set(new GameName("HelloWorld"));
			// 'Inject' the configuration data that will be used for 'CreateEntityThatWillPrintSystem'
			game.Global.Context.BindExisting(new PrintConfiguration
			{
				TextsToPrint = new []
				{
					"Hello World!"
				}
			});
			
			// Once we've added all required data, setup the game...
			game.Setup();

			while (game.Loop())
			{
				Thread.Sleep(10);
				
				// If the user type something, quit the game
				if (Console.Read() != 0)
					game.CancellationTokenSource.Cancel();
			}
		}
	}
}