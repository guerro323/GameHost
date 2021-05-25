using System;
using System.Reflection;
using DefaultEcs;
using GameHost.Applications;
using HarmonyLib;

namespace GameHost.Utility
{
	// DefaultEcs doesn't clean some data with worlds that don't have 0 as the id.
	// This class mitigate this issue, so that assemblies can be unload without any issues.
	public static class CleanUntrackedWorlds
	{
		public static void Clean()
		{
			var worlds = typeof(World).GetField("Worlds", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as World[];

			var source = typeof(World).Assembly;
			cleanP(source.GetType("DefaultEcs.Technical.Message.ComponentTypeReadMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.EntityDisposingMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.EntityDisposedMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.EntityDisabledMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.EntityEnabledMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.EntityCreatedMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.EntityCopyMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.ComponentReadMessage", true), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.TrimExcessMessage", true), worlds);

			// this is slow, but needed 
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			foreach (var type in asm.GetTypes())
			{
				if (type.IsGenericType)
					continue;

				try
				{
					CleanComponent(type);
				}
				catch
				{
					// ignored
				}
			}
			
			// this one should be the last to be cleaned
			cleanP(source.GetType("DefaultEcs.Technical.Message.WorldDisposedMessage", true), worlds);
			
			GC.Collect();
		}

		private static void cleanP(Type type, World[] worlds)
		{
			var source = typeof(World).Assembly;
			var genType = source.GetType("DefaultEcs.Technical.Publisher`1", true)!
			                    .MakeGenericType(type);
			var actions = genType
			              .GetField("Actions")!
			              .GetValue(null)
				as object[];

			var max = Math.Min(worlds.Length, actions.Length);
			for (var i = 0; i < max; i++)
			{
				if (worlds[i] == null)
					actions[i] = null;
			}
		}

		public static void Clean<T>()
		{
			var worlds = typeof(World).GetField("Worlds", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as World[];
			
			cleanP(typeof(T), worlds);
		}

		public static void CleanComponent(Type type)
		{
			var worlds = typeof(World).GetField("Worlds", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null) as World[];

			var source = typeof(World).Assembly;
			
			cleanP(source.GetType("DefaultEcs.Technical.Message.ComponentRemovedMessage`1", true)!.MakeGenericType(type), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.ComponentDisabledMessage`1", true)!.MakeGenericType(type), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.ComponentAddedMessage`1", true)!.MakeGenericType(type), worlds);
			cleanP(source.GetType("DefaultEcs.Technical.Message.ComponentEnabledMessage`1", true)!.MakeGenericType(type), worlds);
		}
	}
}