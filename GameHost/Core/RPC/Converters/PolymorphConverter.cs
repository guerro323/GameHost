using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GameHost.Core.RPC.Converters
{
	public abstract class PolymorphConverterBase<TBase> : JsonConverter<TBase>
	{
		public readonly string                TypePtr;
		public readonly JsonSerializerOptions Options;

		public Type Type { get; protected set; }

		public PolymorphConverterBase(string typePtr, JsonSerializerOptions options = null)
		{
			TypePtr = typePtr;
			Options = options;
		}

		public override bool CanConvert(Type type)
		{
			return typeof(TBase).IsAssignableFrom(type);
		}

		public bool TryConvert(JsonDocument doc, string typeDiscriminator, out TBase item)
		{
			item = default;
			if (typeDiscriminator != this.TypePtr)
				return false;

			item = (TBase) JsonSerializer.Deserialize(doc.RootElement.GetRawText(), Type, Options);
			return true;
		}
	}

	public class PolymorphConverter<TBase, TOut> : PolymorphConverterBase<TBase>
		where TOut : TBase
	{
		public override TBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			using var doc               = JsonDocument.ParseValue(ref reader);
			var       typeDiscriminator = doc.RootElement.GetProperty("type").GetString();
			if (!TryConvert(doc, typeDiscriminator, out var item))
			{
				foreach (var converter in options.Converters)
					if (converter is PolymorphConverterBase<TBase> itemConverter && itemConverter.TryConvert(doc, typeDiscriminator, out item))
						return item;
			}

			return item;
		}

		public override void Write(Utf8JsonWriter writer, TBase value, JsonSerializerOptions options)
		{
		}

		public PolymorphConverter(string typePtr, JsonSerializerOptions options = null) : base(typePtr, options)
		{
			Type = typeof(TOut);
		}
	}
}