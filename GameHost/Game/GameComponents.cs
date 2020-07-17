using GameHost.Core.IO;
using Microsoft.Extensions.Logging;

namespace GameHost.Game
{
	public readonly struct GameName
	{
		public readonly string Value;

		public GameName(string value)
		{
			this.Value = value;
		}
	}

	/// <summary>
	/// Directory of where the game is currently executed.
	/// </summary>
	public readonly struct GameExecutingStorage
	{
		public readonly IStorage Value;

		public GameExecutingStorage(IStorage value)
		{
			this.Value = value;
		}
	}

	/// <summary>
	/// Directory of user game data.
	/// </summary>
	public readonly struct GameUserStorage
	{
		public readonly IStorage Value;

		public GameUserStorage(IStorage value)
		{
			this.Value = value;
		}
	}

	public readonly struct GameLogger
	{
		public readonly ILogger Value;

		public GameLogger(ILogger value)
		{
			Value = value;
		}
	}

	public readonly struct GameLoggerFactory
	{
		public readonly ILoggerFactory Value;

		public GameLoggerFactory(ILoggerFactory value)
		{
			this.Value = value;
		}
	}
}