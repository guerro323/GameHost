using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DefaultEcs;
using GameHost.Applications;
using GameHost.Core.Ecs;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public class RpcSystem : AppSystem
	{
		private readonly Dictionary<string, EntityRpcMultiHandler> handlers = new();

		private readonly Dictionary<Type, string> packetTypeToMethodNames = new();

		public RpcSystem(WorldCollection collection) : base(collection)
		{
		}

		public Entity CreateClient(Action<Entity> onSendPacket, bool isPublic = true)
		{
			var client = World.Mgr.CreateEntity();
			client.Set(new EntityRpcClientInvokeOnSendPacket(onSendPacket));
			if (isPublic)
				client.Set(new EntityRpcPublicClient());

			return client;
		}

		public IDisposable RegisterPacket<T>(string methodName = null)
			where T : IGameHostRpcPacket
		{
			methodName ??= typeof(T).Namespace + "." + typeof(T).Name;

			var packetHandler = new DefaultHandler<T>(methodName);
			handlers[packetHandler.Method] = new(packetHandler, null);

			packetTypeToMethodNames[typeof(T)] = methodName;

			return new RemoveDictionaryKey {Key = methodName, Dictionary = handlers};
		}

		public IDisposable RegisterPacketWithResponse<T, TResponse>(string methodName = null)
			where T : IGameHostRpcWithResponsePacket<TResponse>
			where TResponse : IGameHostRpcResponsePacket
		{
			methodName ??= typeof(T).Namespace + "." + typeof(T).Name;

			var packetHandler   = new DefaultHandler<T>(methodName);
			var responseHandler = new ResponseDefaultHandler<TResponse>(methodName);
			handlers[packetHandler.Method] = new(packetHandler, responseHandler);

			packetTypeToMethodNames[typeof(T)]         = methodName;
			packetTypeToMethodNames[typeof(TResponse)] = methodName;

			return new RemoveDictionaryKey {Key = methodName, Dictionary = handlers};
		}

		public RpcCallEntity<TResponse> CreateCall<T, TResponse>(in T call, Entity client = default, bool disposeOnGetResponse = true)
			where T : IGameHostRpcWithResponsePacket<TResponse>
			where TResponse : IGameHostRpcResponsePacket
		{
			var entity = World.Mgr.CreateEntity();
			entity.Set(call);
			entity.Set(new EntityRpcMultiHandler(
				handlers[packetTypeToMethodNames[typeof(T)]].Request,
				handlers[packetTypeToMethodNames[typeof(TResponse)]].Response
			));
			entity.Set(new RpcReceivedPacketInvokableList<TResponse>());

			if (client.IsAlive)
				entity.Set(new EntityRpcTargetClient(client));

			if (disposeOnGetResponse)
				entity.Set<DestroyOnProcessedTag>();
			entity.Set<RequireServerReplyTag>();

			return new(entity);
		}

		public RpcCallEntity<TResponse> CreateCall<T, TResponse>(Entity client = default, bool disposeOnGetResponse = true)
			where T : IGameHostRpcWithResponsePacket<TResponse>, new()
			where TResponse : IGameHostRpcResponsePacket
		{
			return CreateCall<T, TResponse>(new(), client, disposeOnGetResponse);
		}

		public Entity CreateNotification<T>(in T call, Entity client = default)
		{
			var entity = World.Mgr.CreateEntity();
			entity.Set(call);
			entity.Set(new EntityRpcMultiHandler(
				handlers[packetTypeToMethodNames[typeof(T)]].Request,
				null
			));
			if (client.IsAlive)
				entity.Set(new EntityRpcTargetClient(client));

			entity.Set<DestroyOnProcessedTag>();

			return entity;
		}

		public Entity AddIncomingRequest(string method, JsonElement element, Entity clientEntity)
		{
			var entity = World.Mgr.CreateEntity();
			entity.Set<DestroyOnProcessedTag>();
			entity.Set<ClientRequestTag>();
			entity.Set(new EntityRpcTargetClient(clientEntity));

			handlers[method].Request.Receive(entity, element);

			return entity;
		}

		public void AddIncomingResponse(Entity entity, string method, JsonElement element, Entity clientEntity)
		{
			entity.Set<DestroyOnProcessedTag>();

			handlers[method].Response.Receive(entity, element);

			entity.Remove<RequireServerReplyTag>();
		}

		public Entity AddIncomingNotification(string method, JsonElement element, Entity clientEntity)
		{
			var entity = World.Mgr.CreateEntity();
			entity.Set<NotificationTag>();
			entity.Set<DestroyOnProcessedTag>();
			entity.Set(new EntityRpcTargetClient(clientEntity));

			handlers[method].Request.Receive(entity, element);

			return entity;
		}


		public RpcClientRequestEntity<TRequest, TResponse> PrepareReply<TRequest, TResponse>(Entity request)
			where TRequest : IGameHostRpcWithResponsePacket<TResponse>
			where TResponse : IGameHostRpcResponsePacket
		{
			return new(request, handlers[packetTypeToMethodNames[typeof(TResponse)]].Response);
		}

		private class RemoveDictionaryKey : IDisposable
		{
			public Dictionary<string, EntityRpcMultiHandler> Dictionary;
			public string                                    Key;

			public void Dispose()
			{
				Dictionary.Remove(Key);
			}
		}

		public struct DestroyOnProcessedTag
		{
		}

		public struct RequireServerReplyTag
		{
		}

		public struct ClientRequestTag
		{
		}

		public struct NotificationTag
		{
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
		public readonly IPacketRpcHandler Request, Response;

		public EntityRpcMultiHandler(IPacketRpcHandler request, IPacketRpcHandler response)
		{
			Request  = request;
			Response = response;
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

	public readonly struct RpcCallEntity<TResponse> : IDisposable
		where TResponse : IGameHostRpcResponsePacket
	{
		public readonly Entity Entity;

		public bool HasResponse => Entity.Has<TResponse>();
		public bool HasError    => Entity.Has<RpcPacketError>();

		public TResponse Response
		{
			get
			{
				var data = Entity.Get<TResponse>();
				if (Entity.Has<RpcSystem.DestroyOnProcessedTag>())
					Entity.Dispose();
				return data;
			}
		}

		public RpcPacketError Error
		{
			get
			{
				var data = Entity.Get<RpcPacketError>();
				if (Entity.Has<RpcSystem.DestroyOnProcessedTag>())
					Entity.Dispose();
				return data;
			}
		}

		public RpcCallEntity(Entity entity)
		{
			Entity = entity;
		}

		public event Action<OnRpcReceivedPacket<TResponse>> OnReply
		{
			add => Entity.Get<RpcReceivedPacketInvokableList<TResponse>>().Add(value);
			remove => Entity.Get<RpcReceivedPacketInvokableList<TResponse>>().Remove(value);
		}

		public void Dispose()
		{
			Entity.Dispose();
		}
	}

	public readonly struct RpcClientRequestEntity<TRequest, TResponse>
		where TRequest : IGameHostRpcWithResponsePacket<TResponse>
		where TResponse : IGameHostRpcResponsePacket
	{
		public readonly Entity            Entity;
		public readonly IPacketRpcHandler HandlerToSet;

		public TRequest Request => Entity.Get<TRequest>();

		public RpcClientRequestEntity(Entity entity, IPacketRpcHandler handlerToSet)
		{
			Entity       = entity;
			HandlerToSet = handlerToSet;
		}

		public void ReplyWith(TResponse response)
		{
			if (!Entity.Has<RpcSystem.ClientRequestTag>())
				throw new InvalidOperationException($"no '{nameof(RpcSystem.ClientRequestTag)}' found on {Entity}");

			Entity.Set(response);

			Entity.Remove<RpcPacketError>();
			Entity.Set(new EntityRpcMultiHandler(
				null,
				HandlerToSet
			));

			Entity.Set<RpcSystem.DestroyOnProcessedTag>();
			Entity.Remove<RpcSystem.ClientRequestTag>();
		}

		public void SetError(RpcPacketError error)
		{
			if (!Entity.Has<RpcSystem.ClientRequestTag>())
				throw new InvalidOperationException($"no '{nameof(RpcSystem.ClientRequestTag)}' found on {Entity}");

			Entity.Remove<TResponse>();
			Entity.Set(error);

			Entity.Set<RpcSystem.DestroyOnProcessedTag>();
			Entity.Remove<RpcSystem.ClientRequestTag>();
		}
	}

	public class RpcReceivedPacketInvokableList<T> : List<Action<OnRpcReceivedPacket<T>>>
		where T : IGameHostRpcPacket
	{
	}

	public readonly struct OnRpcReceivedPacket<T>
		where T : IGameHostRpcPacket
	{
		public readonly Entity Packet;
		public readonly T      Value;
		public readonly Entity Replier;

		public OnRpcReceivedPacket(Entity packet, T value, Entity replier)
		{
			Packet  = packet;
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

	public class DefaultHandler<T> : IPacketRpcHandler
		where T : IGameHostRpcPacket
	{
		public DefaultHandler(string method)
		{
			Method = method;
		}

		public string Method { get; }

		public JsonSerializerOptions Options = new ()
		{
			IncludeFields = true,
		};

		public virtual void Send(Entity entity, Utf8JsonWriter writer)
		{
			var t = entity.Get<T>();
			JsonSerializer.Serialize(writer, t, Options);
		}

		public virtual void Receive(Entity entity, JsonElement element)
		{
			entity.Set(JsonSerializer.Deserialize<T>(element.ToString(), Options));
		}
	}

	public class ResponseDefaultHandler<T> : DefaultHandler<T>
		where T : IGameHostRpcPacket
	{
		public ResponseDefaultHandler(string method) : base(method)
		{
		}

		public override void Receive(Entity entity, JsonElement element)
		{
			base.Receive(entity, element);

			var ev = new OnRpcReceivedPacket<T>(entity, entity.Get<T>(), entity.Get<EntityRpcTargetClient>().Client);
			entity.World.Publish(ev);

			if (entity.TryGet(out RpcReceivedPacketInvokableList<T> invokableList))
			{
				foreach (var action in invokableList)
					action(ev);

				if (invokableList.Any() && entity.Has<RpcSystem.DestroyOnProcessedTag>())
					entity.Dispose();
			}
		}
	}

	public struct RpcPacketError
	{
		[JsonPropertyName("code")]
		public int Code { get; set; }

		[JsonPropertyName("message")]
		public string Message { get; set; }

		public RpcPacketError(int code)
		{
			Code    = code;
			Message = string.Empty;
		}

		public RpcPacketError(int code, string message)
		{
			Code    = code;
			Message = message;
		}
	}

	public struct NoMembersResponsePacket : IGameHostRpcResponsePacket
	{
	}
}