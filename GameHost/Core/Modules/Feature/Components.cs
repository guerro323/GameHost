﻿using System.Text.Json.Serialization;
using DefaultEcs;

namespace GameHost.Core.Modules.Feature
{
	public struct RefreshModuleList
	{
	}

	public struct RegisteredModule
	{
		public GameHostModuleDescription Description;
		public ModuleState               State;
	}

	public enum ModuleState
	{
		None,
		IsLoading,
		Loaded,
		Unloading,
		Zombie
	}

	public readonly struct RequestLoadModule
	{
		public readonly string Name;
		public readonly Entity Module;

		public RequestLoadModule(string name, Entity module)
		{
			Name   = name;
			Module = module;
		}
	}

	public struct RequestUnloadModule
	{
		public Entity Module;
	}

	public struct ModuleConfigurationFile
	{
		/// <summary>
		/// Automatically load the Module when <see cref="GatherAvailableModuleSystem"/> has found it.
		/// </summary>
		[JsonPropertyName("autoLoad")]
		public bool AutoLoad { get; set; }
	}
}