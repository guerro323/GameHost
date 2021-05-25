using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Utility;
using Microsoft.Extensions.Logging;
using SharpInputSystem;
using SharpInputSystem.DirectX;
using ZLogger;

namespace GameHost.Inputs.Systems
{
	//[DontInjectSystemToWorld]
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
			
			_kb?.Dispose();
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

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

						(_kb = inputManager.CreateInputObject<Keyboard>(true, "")).EventListener = new KeyboardListener(backendSystem);

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
			public readonly InputBackendSystem Backend;

			public KeyboardListener(InputBackendSystem backendSystem)
			{
				Backend = backendSystem;

				foreach (KeyCode e in Enum.GetValues(typeof(KeyCode)))
				{
					var key = $"keyboard/{e.ToString()!.ToLower().Replace("key_", string.Empty)}";
					
					ControlMap[e] = Backend.GetOrCreateInputControl(key);
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