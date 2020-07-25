using System;
using System.Collections.Generic;
using System.Diagnostics;
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
				list.Add(typeof(UpdateSoLoudBackendDriverSystem));
				return list;
			}
		}

		[Test]
		public void Test1()
		{
			var serverDriver = new ThreadTransportDriver(16);
			serverDriver.Listen();

			Server.Data.World.CreateEntity().Set<IFeature>(new SoLoudBackendFeature(serverDriver));
			Client.Data.World.CreateEntity().Set<IFeature>(new AudioClientFeature(serverDriver.TransportAddress.Connect(), default));

			var request = Client.Data.World.CreateEntity();
			var str = "Hello World!";
			request.Set(new Request {Value = str});

			Global.Loop();
			Global.Loop();

			
			Assert.AreEqual(str, Server.Data.World.Get<Request>()[0].Value);
		}

		public struct Request
		{
			public string Value;
		}
	}
}