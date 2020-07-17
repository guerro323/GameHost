using System;
using GameHost.Game;
using NUnit.Framework;

namespace GameHost.Tests
{
	public class GameTests
	{
		[Test]
		public void CreateGame()
		{
			using (var game = new GameBootstrap())
			{
				game.GameEntity.Set(new GameName("GameTest"));
				game.CancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(0.1));
				game.Run();
			}
			
			Assert.Pass();
		}
	}
}