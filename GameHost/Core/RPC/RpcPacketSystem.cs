using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Client;
using GameHost.Core.Ecs;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	[UpdateAfter(typeof(StartGameHostListener))]
	public abstract class RpcPacketSystem<T> : AppSystem
		where T : IGameHostRpcPacket
	{
		protected RpcSystem RpcSystem;

		private readonly EntitySet notificationSet;
		
		protected RpcPacketSystem(WorldCollection collection) : base(collection)
		{
			notificationSet = World.Mgr.GetEntities()
			                       .With<T>()
			                       .With<RpcSystem.NotificationTag>()
			                       .AsSet();
			
			DependencyResolver.Add(() => ref RpcSystem);
		}

		public abstract string MethodName { get; }

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			RpcSystem.RegisterPacket<T>(MethodName);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			
			foreach (ref readonly var entity in notificationSet.GetEntities())
				OnNotification(entity.Get<T>());
		}

		protected abstract void OnNotification(T notification);
	}

	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public abstract class RpcPacketWithResponseSystem<T, TResponse> : AppSystem
		where T : IGameHostRpcWithResponsePacket<TResponse>
		where TResponse : IGameHostRpcResponsePacket
	{
		private readonly EntitySet awaitingResponseSet;

		private RpcPacketError? lastError;

		protected RpcSystem RpcSystem;

		protected RpcPacketWithResponseSystem(WorldCollection collection) : base(collection)
		{
			awaitingResponseSet = collection.Mgr.GetEntities()
			                                .With<TResponse>()
			                                .With<RpcSystem.ClientRequestTag>()
			                                .AsSet();

			DependencyResolver.Add(() => ref RpcSystem);
		}

		public abstract string MethodName { get; }

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			RpcSystem.RegisterPacketWithResponse<T, TResponse>(MethodName);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (ref readonly var entity in awaitingResponseSet.GetEntities())
			{
				lastError = null;

				var rpcRequest = RpcSystem.PrepareReply<T, TResponse>(entity);
				var response   = GetResponse(rpcRequest.Request);

				if (lastError is { } packetError)
					rpcRequest.SetError(packetError);
				else
					rpcRequest.ReplyWith(response);
			}
		}

		protected abstract TResponse GetResponse(in T request);

		protected TResponse WithError(RpcPacketError packet)
		{
			lastError = packet;
			return default;
		}

		protected TResponse WithError(int code, string message)
		{
			lastError = new RpcPacketError(code, message);
			return default;
		}
	}
}