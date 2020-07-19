using System;
using System.Collections.Generic;
using GameHost.Applications;
using GameHost.Audio.Features;
using GameHost.Core.IO;
using GameHost.Transports;
using NUnit.Framework;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Audio.Tests
{
	public class Tests : ApplicationBase
	{
		public override List<Type> RequiredAudioSystems
		{
			get
			{
				var list = base.RequiredAudioSystems ?? new List<Type>();
				list.Add(typeof(UpdateClientAudioDriver));
				list.Add(typeof(UpdateAudioBackendDriverSystem));
				list.Add(typeof(TransportableData));
				return list;
			}
		}

		[Test]
		public void Test1()
		{
			var serverDriver = new ThreadTransportDriver(16);
			serverDriver.Listen();

			Server.Data.World.CreateEntity().Set<IFeature>(new SoLoudBackendFeature(serverDriver));
			Client.Data.World.CreateEntity().Set<IFeature>(new ClientAudioFeature(serverDriver.TransportAddress.Connect(), default));

			var transportableData = Client.Data.Collection.GetOrCreate(c => new TransportableData(c));
			
			var request = Client.Data.World.CreateEntity();
			request.Set<ClientAudioFeature.SendRequest>();
			
			transportableData.Serialize(request, new Request {Value = "hello world!"});

			Global.Loop();
			Global.Loop();
			Global.Loop();

			Console.WriteLine(Server.Data.World.Get<Request>()[0].Value);
		}
		
		public struct Request : ITransportableData
		{
			public string Value;

			public int GetCapacity()
			{
				return Value.Length;
			}

			public void Serialize(ref DataBufferWriter writer)
			{
				writer.WriteStaticString(Value);
			}

			public void Deserialize(ref DataBufferReader reader)
			{
				Value = reader.ReadString();
			}
		}
	}
}