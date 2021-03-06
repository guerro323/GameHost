﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Client;
using GameHost.Core.Ecs;
using GameHost.Core.Threading;
using GameHost.Utility;
using Microsoft.Extensions.Logging;
using ZLogger;

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

		protected RpcSystem     RpcSystem;

		private ILogger logger;
		protected RpcPacketWithResponseSystem(WorldCollection collection) : base(collection)
		{
			awaitingResponseSet = collection.Mgr.GetEntities()
			                                .With<T>()
			                                .With<RpcSystem.ClientRequestTag>()
			                                .Without<Task>()
			                                .AsSet();

			DependencyResolver.Add(() => ref RpcSystem);
			DependencyResolver.Add(() => ref logger);
		}

		public abstract string MethodName { get; }

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);

			RpcSystem.RegisterPacketWithResponse<T, TResponse>(MethodName);
		}

		private IScheduler    scheduler = new Scheduler();
		private TaskScheduler taskScheduler = new SameThreadTaskScheduler();
		
		protected override void OnUpdate()
		{
			base.OnUpdate();

			foreach (ref readonly var entity in awaitingResponseSet.GetEntities())
			{
				lastError = null;

				var rpcRequest = RpcSystem.PrepareReply<T, TResponse>(entity);
				scheduler.Schedule(ent =>
				{
					ent.Entity.Set<Task>(taskScheduler.StartUnwrap(() => PrivateGetResponse(ent)));
				}, rpcRequest, default);
			}

			scheduler.Run();
			(taskScheduler as SameThreadTaskScheduler).Execute();
		}
		
		private async Task PrivateGetResponse(RpcClientRequestEntity<T, TResponse> currentRequest)
		{
			try
			{
				var response = await GetResponse(currentRequest.Request);
				if (lastError is { } error)
				{
					logger.ZLogError("Code {0}: {1}", error.Code, error.Message);
					currentRequest.SetError(error);
				}
				else
					currentRequest.ReplyWith(response);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		protected abstract ValueTask<TResponse> GetResponse(T request);

		protected ValueTask<TResponse> WithError(RpcPacketError packet)
		{
			lastError = packet;
			return ValueTask.FromResult(default(TResponse));
		}

		protected ValueTask<TResponse> WithError(int code, string message)
		{
			lastError = new RpcPacketError(code, message);
			return ValueTask.FromResult(default(TResponse));
		}

		protected ValueTask<TResponse> WithResult(TResponse response) => ValueTask.FromResult(response);
	}
}