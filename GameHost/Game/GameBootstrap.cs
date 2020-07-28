using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Text;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.IO;
using GameHost.Threading;
using GameHost.Worlds;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace GameHost.Game
{
	public class GameBootstrap : IDisposable
	{
		public readonly CancellationTokenSource CancellationTokenSource;
		public readonly GlobalWorld             Global;

		public readonly Entity GameEntity;
		public readonly Entity DefaultListenerCollection;

		public GameBootstrap()
		{
			CancellationTokenSource = new CancellationTokenSource();
			Global                  = new GlobalWorld();

			// Create Game Entity
			GameEntity = Global.World.CreateEntity();

			// Create Default Listener collection
			DefaultListenerCollection = Global.World.CreateEntity();
			DefaultListenerCollection.Set<ListenerCollectionBase>(new ListenerCollection());

			AppDomain.CurrentDomain.UnhandledException += (sender, args) => { OnException((Exception) args.ExceptionObject); };
			TaskScheduler.UnobservedTaskException      += (sender, args) => { OnException((Exception) args.Exception); };
		}

		public void Setup()
		{
			if (!GameEntity.Has<GameName>())
				throw new InvalidOperationException("A game name should be set before running.");

			if (Thread.CurrentThread.Name == null)
				Thread.CurrentThread.Name = $"{GameEntity.Get<GameName>().Value}";

			void ResolveDefaults()
			{
				if (!GameEntity.Has<GameExecutingStorage>())
					GameEntity.Set(new GameExecutingStorage(new LocalStorage(Environment.CurrentDirectory)));

				if (!GameEntity.Has<GameLoggerFactory>())
				{
					if (GameEntity.Has<GameLogger>())
						throw new InvalidOperationException("Game has no ILoggerFactory, but has a ILogger, either include both or none.");

					var loggerFactory = LoggerFactory.Create(builder =>
					{
						static void opt(ZLoggerOptions options)
						{
							var prefixFormat = ZString.PrepareUtf8<LogLevel, DateTime, string>("[{0}, {1}, {2}] ");
							options.PrefixFormatter = (writer, info) => prefixFormat.FormatTo(ref writer, info.LogLevel, info.Timestamp.DateTime.ToLocalTime(), info.CategoryName);
						}

						builder.ClearProviders();
						builder.SetMinimumLevel(LogLevel.Debug);
						builder.AddZLoggerRollingFile((offset, i) => $"logs/{offset.ToLocalTime():yyyy-MM-dd}_{i:000}.log",
							x => x.ToLocalTime().Date,
							8196,
							opt);
						builder.AddZLoggerFile("log.json");
						builder.AddZLoggerConsole(opt);
					});
					GameEntity.Set(new GameLoggerFactory(loggerFactory));
				}

				if (!GameEntity.Has<GameLogger>())
				{
					GameEntity.Set(new GameLogger(GameEntity.Get<GameLoggerFactory>().Value.CreateLogger("Game")));
				}
			}

			void SetContexts()
			{
				Global.Context.BindExisting(GameEntity.Get<GameLogger>().Value);
				Global.Context.BindExisting(GameEntity.Get<GameLoggerFactory>().Value);
				Global.Context.BindExisting(new AssemblyLoadContext("DefaultContext"));
				
				if (GameEntity.TryGet(out GameUserStorage userStorage))
					Global.Context.BindExisting(userStorage.Value);
			}

			void AddExecutiveSystems()
			{
				var foundEntrySystemTypes = new List<Type>(16);
				AppSystemResolver.ResolveFor<ExecutiveEntryApplication>(foundEntrySystemTypes,
					t => t.GetCustomAttribute<RestrictToApplicationAttribute>()?.IsValid<ExecutiveEntryApplication>() == true);

				foreach (var systemType in foundEntrySystemTypes)
					Global.Collection.GetOrCreate(systemType);
			}

			ResolveDefaults();
			SetContexts();
			AddExecutiveSystems();
		}

		public bool Loop()
		{
			if (CancellationTokenSource.IsCancellationRequested)
				return false;
			
			Global.Loop();
			return true;
		}

		public void Dispose()
		{
			CancellationTokenSource.Cancel();
			Global.Scheduler.Run();
			Global.Collection.Dispose();
		}


		private void OnException(Exception ex)
		{
			try
			{
				if (GameEntity.IsAlive && GameEntity.Has<GameLogger>())
				{
					GameEntity.Get<GameLogger>().Value.ZLogError(ex, "Unhandled Exception.");
				}
			}
			catch
			{
				try
				{
					Console.WriteLine("Unhandled Exception.");
				}
				catch
				{
					// ignored, we can't write to the console.
				}
			}

			// quit game
			CancellationTokenSource.Cancel();
		}
	}
}