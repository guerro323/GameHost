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
		private Dictionary<string, InputActionSystemBase> SystemActionMap;
		private Dictionary<string, InputActionSystemBase> SystemLayoutMap;

		public InputActionSystemGroup(WorldCollection collection) : base(collection)
		{
			SystemActionMap = new Dictionary<string, InputActionSystemBase>();
			SystemLayoutMap = new Dictionary<string, InputActionSystemBase>();
		}

		public Dictionary<string,InputActionSystemBase>.ValueCollection Systems => SystemActionMap.Values;

		public void Add(InputActionSystemBase system)
		{
			SystemActionMap.Add(system.ActionPath, system);
			SystemLayoutMap.Add(system.LayoutPath, system);
		}

		public InputActionSystemBase GetSystemOrDefault(string type)
		{
			if (!SystemActionMap.TryGetValue(type, out var system))
				SystemLayoutMap.TryGetValue(type, out system);
			return system;
		}

		public void BackendUpdateInputs()
		{
			foreach (var system in SystemActionMap)
			{
				if (system.Value is IUpdateInputPass pass)
					pass.OnInputUpdate();
			}
		}

		public void BeginFrame()
		{
			foreach (var system in SystemActionMap.Values)
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

		public abstract void CallSerialize(ref   DataBufferWriter buffer);
		public abstract void CallDeserialize(ref DataBufferReader buffer);
		public abstract void OnBeginFrame();

		public abstract Entity          CreateEntityAction();
		public abstract InputLayoutBase CreateLayout(string layoutId);
	}

	public abstract class InputActionSystemBase<TAction, TLayout> : InputActionSystemBase, IUpdateInputPass
		where TAction : struct, IInputAction
		where TLayout : InputLayoutBase
	{
		protected InputBackendSystem Backend;
		
		public override Entity CreateEntityAction()
		{
			var ent = World.Mgr.CreateEntity();
			ent.Set<TAction>();
			return ent;
		}

		public override InputLayoutBase CreateLayout(string layoutId)
		{
			return (InputLayoutBase) Activator.CreateInstance(typeof(TLayout), layoutId);
		}

		public override string LayoutPath => CustomLayoutPath ?? typeof(TLayout).FullName;
		public override string ActionPath => CustomActionPath ?? typeof(TAction).FullName;
		
		private InputActionSystemGroup group;
		private ReceiveInputDataSystem receiveSystem;

		protected EntitySet InputQuery { get; }

		protected InputActionSystemBase(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref group);
			DependencyResolver.Add(() => ref receiveSystem);
			DependencyResolver.Add(() => ref Backend);

			InputQuery = collection.Mgr.GetEntities()
			                       .With<TAction>()
			                       .With<InputEntityId>()
			                       .AsSet();
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			group.Add(this);
		}

		public override void CallSerialize(ref DataBufferWriter buffer)
		{
			OnSerialize(ref buffer);
		}

		public override void CallDeserialize(ref DataBufferReader buffer)
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
				var data   = default(TAction);
				data.Deserialize(ref buffer);
				
				foreach (ref readonly var entity in InputQuery.GetEntities())
				{
					if (entity.Get<InputEntityId>().Value != replId)
						continue;

					ref var current = ref entity.Get<TAction>();
					current = data;

					break;
				}
			}
		}
		
		protected InputActionLayouts GetLayouts(in Entity entity)
		{
			return Backend.GetLayoutsOf(entity);
		}

		public override void OnBeginFrame()
		{
			foreach (ref readonly var entity in InputQuery.GetEntities())
				entity.Get<TAction>() = default;
		}

		public abstract void OnInputUpdate();
	}
}