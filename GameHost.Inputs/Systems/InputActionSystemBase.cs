using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Core.Ecs;
using GameHost.Inputs.Components;
using GameHost.Inputs.Interfaces;
using GameHost.Inputs.Layouts;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.Systems
{
	public class InputActionSystemGroup : AppSystem
	{
		private Dictionary<string, InputActionSystemBase> SystemMap;

		public InputActionSystemGroup(WorldCollection collection) : base(collection)
		{
			SystemMap = new Dictionary<string, InputActionSystemBase>();
		}

		public void Add(InputActionSystemBase system)
		{
			Console.WriteLine($"Register action system : {system.ActionPath}");
			
			SystemMap.Add(system.ActionPath, system);
		}

		public InputActionSystemBase TryGetSystem(string type)
		{
			SystemMap.TryGetValue(type, out var system);
			return system;
		}

		public void BeginFrame()
		{
			foreach (var system in SystemMap.Values)
				system.OnBeginFrame();
		}
	}

	public abstract class InputActionSystemBase : AppSystem
	{
		public abstract string LayoutPath { get; }
		public abstract string ActionPath { get; }
		
		protected virtual string CustomLayoutPath { get; }
		protected virtual string CustomActionPath { get; }
		
		protected InputActionSystemBase(WorldCollection collection) : base(collection)
		{
		}

		internal abstract  void CallSerialize(ref   DataBufferWriter buffer);
		internal abstract  void CallDeserialize(ref DataBufferReader buffer);
		public abstract void OnBeginFrame();
	}

	public abstract class InputActionSystemBase<TAction, TLayout> : InputActionSystemBase
		where TAction : IInputAction
		where TLayout : InputLayoutBase
	{
		public override string LayoutPath => CustomLayoutPath ?? typeof(TLayout).FullName;
		public override string ActionPath => CustomActionPath ?? typeof(TAction).FullName;
		
		private InputActionSystemGroup group;
		private ReceiveInputDataSystem receiveSystem;

		protected EntitySet InputQuery { get; }

		protected InputActionSystemBase(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref group);
			DependencyResolver.Add(() => ref receiveSystem);

			InputQuery = collection.Mgr.GetEntities()
			                       .With<TAction>()
			                       .With<InputActionLayouts>()
			                       .With<InputEntityId>()
			                       .AsSet();
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			group.Add(this);
		}

		internal override void CallSerialize(ref DataBufferWriter buffer)
		{
			OnSerialize(ref buffer);
		}

		internal override void CallDeserialize(ref DataBufferReader buffer)
		{
			OnDeserialize(ref buffer);
		}

		protected virtual void OnSerialize(ref DataBufferWriter buffer)
		{
			buffer.WriteInt(InputQuery.Count);
			foreach (ref readonly var entity in InputQuery.GetEntities())
			{
				var repl   = entity.Get<InputEntityId>();
				var action = entity.Get<TAction>();

				buffer.WriteInt(repl.Value);
				action.Serialize(ref buffer);
			}
		}

		protected virtual void OnDeserialize(ref DataBufferReader buffer)
		{
			var entityCount = buffer.ReadValue<int>();
			for (var i = 0; i < entityCount; i++)
			{
				var replId = buffer.ReadValue<int>();
				foreach (ref readonly var entity in InputQuery.GetEntities())
				{
					if (entity.Get<InputEntityId>().Value != replId)
						continue;

					ref var current = ref entity.Get<TAction>();
					current.Deserialize(ref buffer);

					break;
				}
			}
		}

		public override void OnBeginFrame()
		{
			foreach (ref readonly var entity in InputQuery.GetEntities())
				entity.Get<TAction>() = default;
		}
	}
}