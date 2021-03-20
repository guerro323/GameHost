using System;
using System.Collections.Generic;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Client;
using GameHost.Core.Ecs;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public abstract class RpcPacketSystem<T> : AppSystem
		where T : IGameHostRpcPacket
	{
		public abstract string MethodName { get; }

		protected RpcSystem RpcSystem;

		protected RpcPacketSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref RpcSystem);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			RpcSystem.RegisterPacket<T>(MethodName);
		}
	}

	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public abstract class RpcPacketWithResponseSystem<T, TResponse> : AppSystem
		where T : IGameHostRpcWithResponsePacket<TResponse>
		where TResponse : IGameHostRpcResponsePacket
	{
		public abstract string MethodName { get; }

		protected RpcSystem RpcSystem;

		private EntitySet awaitingResponseSet;

		protected RpcPacketWithResponseSystem(WorldCollection collection) : base(collection)
		{
			awaitingResponseSet = collection.Mgr.GetEntities()
			                                .With<TResponse>()
			                                .With<RpcSystem.ClientRequestTag>()
			                                .AsSet();

			DependencyResolver.Add(() => ref RpcSystem);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			RpcSystem.RegisterPacketWithResponse<T, TResponse>(MethodName);
		}

		private RpcPacketError? lastError;

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