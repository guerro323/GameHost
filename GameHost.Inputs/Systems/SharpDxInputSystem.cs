using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Common.Logging;
using Common.Logging.Simple;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Utility;
using Microsoft.Extensions.Logging;
using SharpDX;
using SharpInputSystem;
using SharpInputSystem.DirectX;
using ZLogger;
using LogLevel = Common.Logging.LogLevel;

namespace GameHost.Inputs.Systems
{
	[DontInjectSystemToWorld]
	public class SharpDxInputSystem : AppSystem, IUpdateInputPass
	{
		private InputBackendSystem backendSystem;
		private IScheduler      scheduler;
		private TaskScheduler      taskScheduler;
		private ILogger            logger;
		
		private InputManager inputManager;
		
		public SharpDxInputSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref backendSystem);
			DependencyResolver.Add(() => ref scheduler);
			DependencyResolver.Add(() => ref taskScheduler);
			DependencyResolver.Add(() => ref logger);
		}

		private Keyboard _kb;

		public override void Dispose()
		{
			base.Dispose();

			if (_kb is { } kb)
			{
				logger.LogWarning("Disposing kb");
				
				kb.EventListener = null;
				kb.Dispose();
				
				logger.LogWarning("Disposing kb end");
			}
			
			foreach (var ev in adapter.LoggerEvents)
			{
				logger.Log(ev.Level switch
				{
					LogLevel.All => Microsoft.Extensions.Logging.LogLevel.Information,
					LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
					LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
					LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
					LogLevel.Warn => Microsoft.Extensions.Logging.LogLevel.Warning,
					LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
					LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Error,
					_ => throw new ArgumentOutOfRangeException()
				}, ev.RenderedMessage);
			}
			
			adapter.Clear();
		}

		private CapturingLoggerFactoryAdapter adapter;
		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			LogManager.Adapter = adapter = new CapturingLoggerFactoryAdapter();

			ComObject.LogMemoryLeakWarning = s => { logger.LogError(s); }; 

			TaskRunUtility.StartUnwrap(async cc =>
			{
				logger.ZLogInformation("Tryin' to get MainWindowHandle");
				var process = Process.GetCurrentProcess();
				while (!cc.IsCancellationRequested)
				{

					if (process.MainWindowHandle == IntPtr.Zero)
					{
						logger.ZLogInformation("Not yet found!");
						await Task.Delay(2500, cc);

						foreach (var other in Process.GetProcesses())
						{
							// If we run on a console app for developping, this mean that there should be a Rider process somewhere, so bind to it.
							if (other.MainWindowHandle != IntPtr.Zero && other.ProcessName.Contains("rider64", StringComparison.InvariantCultureIgnoreCase))
							{
								process = other;
								break;
							}
						}

						continue;
					}

					scheduler.Schedule(() =>
					{ 
						logger.ZLogInformation($"GameHandle (non resolved fully) = {process.MainWindowHandle}");
						
						inputManager = InputManager.CreateInputSystem(typeof(DirectXInputManagerFactory), new ParameterList
						{
							new("WINDOW", Process.GetCurrentProcess().MainWindowHandle)
						});
						
						logger.ZLogInformation($"mouse={inputManager.DeviceCount<SharpInputSystem.Mouse>()}");
						logger.ZLogInformation($"keyboard={inputManager.DeviceCount<SharpInputSystem.Keyboard>()}");

						(_kb = inputManager.CreateInputObject<Keyboard>(true, "")).EventListener = new KeyboardListener(key => backendSystem.GetOrCreateInputControl(key));

					}, default);
					break;
				}
			}, taskScheduler, CancellationToken.None).ContinueWith(t =>
			{
				logger.ZLogError($"Error when trying to attach to window! {t.Exception}");
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		public override bool CanUpdate()
		{
			foreach (var ev in adapter.LoggerEvents)
			{
				logger.Log(ev.Level switch
				{
					LogLevel.All => Microsoft.Extensions.Logging.LogLevel.Information,
					LogLevel.Trace => Microsoft.Extensions.Logging.LogLevel.Trace,
					LogLevel.Debug => Microsoft.Extensions.Logging.LogLevel.Debug,
					LogLevel.Info => Microsoft.Extensions.Logging.LogLevel.Information,
					LogLevel.Warn => Microsoft.Extensions.Logging.LogLevel.Warning,
					LogLevel.Error => Microsoft.Extensions.Logging.LogLevel.Error,
					LogLevel.Fatal => Microsoft.Extensions.Logging.LogLevel.Error,
					_ => throw new ArgumentOutOfRangeException()
				}, ev.RenderedMessage);
			}
			
			adapter.Clear();
			
			return inputManager != null && base.CanUpdate();
		}

		public void OnInputUpdate()
		{
			if (_kb is { } keyboard)
			{
				foreach (var (_, control) in ((KeyboardListener) keyboard.EventListener).ControlMap)
				{
					control.wasPressedThisFrame  = false;
					control.wasReleasedThisFrame = false;
				}

				keyboard.Capture();
			}
		}

		public class KeyboardListener : IKeyboardListener
		{
			public KeyboardListener(Func<string, InputControl> createInputControl)
			{
				foreach (KeyCode e in Enum.GetValues(typeof(KeyCode)))
				{
					var key = $"keyboard/{e.ToString()!.ToLower().Replace("key_", string.Empty)}";
					
					ControlMap[e] = createInputControl(key);
				}
			}

			public Dictionary<KeyCode, InputControl> ControlMap = new(256);

			public bool KeyPressed(KeyEventArgs e)
			{
				var control = ControlMap[e.Key];
				control.axisValue           = 1;
				control.wasPressedThisFrame = true;
				control.isPressed           = true;
				return true;
			}

			public bool KeyReleased(KeyEventArgs e)
			{
				var control = ControlMap[e.Key];
				control.axisValue            = 0;
				control.wasReleasedThisFrame = true;
				control.isPressed            = false;
				return true;
			}
		}
		
		public class KeyboardListenerSimple : IKeyboardListener
		{
			public class Input
			{
				public string Id;
				public bool   IsPressed;
			}
		
			public KeyboardListenerSimple()
			{
				foreach (KeyCode e in Enum.GetValues(typeof(KeyCode)))
				{
					var key = $"keyboard/{e.ToString()!.ToLower().Replace("key_", string.Empty)}";
					ControlMap[e] = new() {Id = key};
				}
			}

			public Dictionary<KeyCode, Input> ControlMap = new(256);

			public bool KeyPressed(KeyEventArgs e)
			{
				var control = ControlMap[e.Key];
				control.IsPressed = true;
				return true;
			}

			public bool KeyReleased(KeyEventArgs e)
			{
				var control = ControlMap[e.Key];
				control.IsPressed = false;
				return true;
			}
		}
	}
}