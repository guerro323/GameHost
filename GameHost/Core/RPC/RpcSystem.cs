using System;
using System.Collections.Generic;
using System.Text.Json;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.IO;
using GameHost.Injection;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RpcSystem : AppSystem
	{
		private class RemoveDictionaryKey : IDisposable
		{
			public string                                Key;
			public Dictionary<string, IPacketRpcHandler> Dictionary;
			
			public void Dispose()
			{
				Dictionary.Remove(Key);
			}
		}
		
		public struct DestroyOnProcessedTag {}
		public struct RequireReplyTag {}
		
		public RpcSystem(WorldCollection collection) : base(collection)
		{
		}

		private Dictionary<Type, string>              packetTypeToMethodNames = new();
		private Dictionary<string, IPacketRpcHandler> handlers = new();

		public Entity CreateClient(Action<Entity> onSendPacket, bool isPublic = true)
		{
			var client = World.Mgr.CreateEntity();
			client.Set(new EntityRpcClientInvokeOnSendPacket(onSendPacket));
			if (isPublic)
				client.Set(new EntityRpcPublicClient());

			return client;
		}

		private IDisposable RegisterDefaultPacket<TIn, TOut>(string methodName)
		{
			var packetHandler = new DefaultHandler<TIn, TOut>(methodName);
			handlers[packetHandler.Method]        = packetHandler;
			packetTypeToMethodNames[typeof(TIn)]  = methodName;
			packetTypeToMethodNames[typeof(TOut)] = methodName;

			return new RemoveDictionaryKey {Key = methodName, Dictionary = handlers};
		}

		public IDisposable RegisterPacket<T>(string methodName = null)
			where T : IGameHostRpcPacket
		{
			return RegisterDefaultPacket<T, T>(methodName ?? typeof(T).Name);
		}
		
		public IDisposable RegisterPacketWithResponse<T, TResponse>(string methodName = null)
			where T : IGameHostRpcWithResponsePacket<TResponse>
			where TResponse : IGameHostRpcResponsePacket
		{
			return RegisterDefaultPacket<T, TResponse>(methodName ?? typeof(T).Name);
		}

		public RpcCallEntity<TResponse> CreateCall<T, TResponse>(in T call, Entity client = default)
			where T : IGameHostRpcWithResponsePacket<TResponse>
			where TResponse : IGameHostRpcResponsePacket
		{
			var entity = World.Mgr.CreateEntity();
			entity.Set(call);
			entity.Set(new EntityRpcMultiHandler(
				send: handlers[packetTypeToMethodNames[typeof(T)]],
				receive: handlers[packetTypeToMethodNames[typeof(TResponse)]]
			));
			entity.Set(new RpcReceivedPacketInvokableList<TResponse>());
			
			if (client.IsAlive)
				entity.Set(new EntityRpcTargetClient(client));
			
			entity.Set<DestroyOnProcessedTag>();
			entity.Set<RequireReplyTag>();

			return new(entity);
		}

		public RpcCallEntity<TResponse> CreateCall<T, TResponse>(Entity client = default)
			where T : IGameHostRpcWithResponsePacket<TResponse>, new()
			where TResponse : IGameHostRpcResponsePacket
		{
			return CreateCall<T, TResponse>(new(), client);
		}

		public Entity CreateNotification<T>(in T call, Entity client = default)
		{
			var entity = World.Mgr.CreateEntity();
			entity.Set(call);
			entity.Set(new EntityRpcMultiHandler(
				send: handlers[packetTypeToMethodNames[typeof(T)]],
				receive: handlers[packetTypeToMethodNames[typeof(T)]]
			));
			if (client.IsAlive)
				entity.Set(new EntityRpcTargetClient(client));
			
			entity.Set<DestroyOnProcessedTag>();
			
			return entity;
		}

		public Entity AddIncomingRequest(string method, JsonElement element)
		{
			var entity = World.Mgr.CreateEntity();
			handlers[method].Receive(entity, element);
			
			entity.Set<DestroyOnProcessedTag>();

			return entity;
		}

		public Entity AddIncomingNotification(string method, JsonElement element)
		{
			var entity = World.Mgr.CreateEntity();
			handlers[method].Receive(entity, element);
			
			entity.Set<DestroyOnProcessedTag>();

			return entity;		
		}

		public void ReplyToRequest(Entity request)
		{
			// TODO: Add component "ReplyToRequestTag" to entity
		}
	}

	public readonly struct EntityRpcTargetClient
	{
		public readonly Entity Client;

		public EntityRpcTargetClient(Entity client)
		{
			Client = client;
		}
	}

	public readonly struct EntityRpcMultiHandler
	{
		public readonly IPacketRpcHandler Send, Receive;

		public EntityRpcMultiHandler(IPacketRpcHandler send, IPacketRpcHandler receive)
		{
			Send    = send;
			Receive = receive;
		}
	}

	public struct EntityRpcPublicClient
	{
	}

	public readonly struct EntityRpcClientInvokeOnSendPacket
	{
		public readonly Action<Entity> OnSendPacket;

		public EntityRpcClientInvokeOnSendPacket(Action<Entity> onSendPacket)
		{
			OnSendPacket = onSendPacket;
		}
	}

	public readonly struct RpcCallEntity<TResponse>
		where TResponse : IGameHostRpcResponsePacket
	{
		public readonly Entity Entity;

		public bool      HasResponse => Entity.Has<TResponse>();
		public TResponse Response    => Entity.Get<TResponse>();

		public RpcCallEntity(Entity entity)
		{
			Entity = entity;
		}
		
		public event Action<OnRpcReceivedPacket<TResponse>> OnReply
		{
			add => Entity.Get<RpcReceivedPacketInvokableList<TResponse>>().Add(value);
			remove => Entity.Get<RpcReceivedPacketInvokableList<TResponse>>().Remove(value);
		}
	}

	public class RpcReceivedPacketInvokableList<T> : List<Action<OnRpcReceivedPacket<T>>>
		where T : IGameHostRpcPacket
	{
	}

	public readonly struct OnRpcReceivedPacket<T>
		where T : IGameHostRpcPacket
	{
		public readonly Entity              Entity;
		public readonly T                   Value;
		public readonly TransportConnection Replier;

		public OnRpcReceivedPacket(Entity entity, T value, TransportConnection replier)
		{
			Entity  = entity;
			Value   = value;
			Replier = replier;
		}
	}

	public interface IPacketRpcHandler
	{
		string Method { get; }

		void Send(Entity    entity, Utf8JsonWriter writer);
		void Receive(Entity entity, JsonElement    element);
	}

	public class DefaultHandler<TIn, TOut> : IPacketRpcHandler
	{
		public string Method { get; }

		public DefaultHandler(string method) => Method = method;
		
		public void Send(Entity entity, Utf8JsonWriter writer)
		{
			var t = entity.Get<TIn>();
			JsonSerializer.Serialize(writer, t);
		}

		public void Receive(Entity entity, JsonElement element)
		{
			entity.Set(JsonSerializer.Deserialize<TOut>(element.ToString()));
		}
	}
}