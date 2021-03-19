using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameHost.Core.RPC;
using GameHost.Core.RPC.Converters;
using Newtonsoft.Json;
using NUnit.Framework;
using RevolutionSnapshot.Core.Buffers;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace GameHost.Tests
{
	public class DeserializeCommandTest
	{
		class Item
		{
			public string type { get; set; }
		}

		class First : Item
		{
			public int first_value { get; set; }
		}

		class Second : Item
		{
			public int second_value { get; set; }
		}

		class Holder
		{
			public List<Item> collection { get; set; }
		}

		/*[Test]
		public void Test()
		{
			var response = new GameHostCommandResponse();
			var str = @"
{
	""collection"": [
		{
			""type"": ""first"",
			""first_value"": 4
		},
		{
			""type"": ""second"",
			""second_value"": 2
		}
	]
}
";
			var writer = new DataBufferWriter(str.Length * sizeof(char) + sizeof(int));
			writer.WriteStaticString(str);

			response.Data = new DataBufferReader(writer);
			Assert.AreEqual(response.Data.ReadString(), str);
			response.Data.CurrReadIndex = 0;

			var deserialized = response.Deserialize<Holder>(new JsonSerializerOptions
			{
				Converters =
				{
					new PolymorphConverter<Item, First>("first"),
					new PolymorphConverter<Item, Second>("second")
				}
			});
			Assert.AreEqual(2, deserialized.collection.Count);
			Assert.AreEqual("first", deserialized.collection[0].type);
			Assert.AreEqual("second", deserialized.collection[1].type);

			Assert.AreEqual(4, ((First) deserialized.collection[0]).first_value);
			Assert.AreEqual(2, ((Second) deserialized.collection[1]).second_value);
		}*/
	}
}