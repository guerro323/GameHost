using System;
using System.Collections.Generic;
using System.Reflection;
using DefaultEcs;
using GameHost.Core.Ecs;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.IO
{
	public interface ITransportableData
	{
		int  GetCapacity();
		void Serialize(ref   DataBufferWriter writer);
		void Deserialize(ref DataBufferReader reader);
	}

	public readonly struct TransportableDataType
	{
		public readonly Type Type;

		public TransportableDataType(Type type)
		{
			Type = type;
		}
	}

	public class TransportableData : AppSystem
	{
		private delegate void CreateComponentDel(Entity entity, ref DataBufferReader reader);

		private Dictionary<Type, CreateComponentDel> createComponentMap;

		private void addComponent<T>(Entity entity, ref DataBufferReader reader)
			where T : ITransportableData, new()
		{
			var data = new T();
			data.Deserialize(ref reader);
			entity.Set(data);
		}

		private void CreateComponent(Type type, Entity entity, ref DataBufferReader reader)
		{
			if (!createComponentMap.TryGetValue(type, out var method))
			{
				method = (CreateComponentDel) GetType().GetMethod(nameof(addComponent), BindingFlags.Instance | BindingFlags.NonPublic)
				                                       .MakeGenericMethod(type)
				                                       .CreateDelegate(typeof(CreateComponentDel), this);
				createComponentMap[type] = method;
			}

			method(entity, ref reader);
		}

		public TransportableData(WorldCollection collection) : base(collection)
		{
			createComponentMap = new Dictionary<Type, CreateComponentDel>();
		}

		public void Serialize<T>(Entity entity, T data)
			where T : ITransportableData
		{
			entity.Set(new TransportableDataType(typeof(T)));

			var buffer = new DataBufferWriter(data.GetCapacity());
			buffer.WriteStaticString(typeof(T).AssemblyQualifiedName);
			data.Serialize(ref buffer);
			entity.Set(buffer);
		}

		public Entity Deserialize(ref DataBufferReader data)
		{
			var entity = World.Mgr.CreateEntity();
			var str = data.ReadString();
			var type   = Type.GetType(str);
			CreateComponent(type, entity, ref data);

			return entity;
		}
	}
}