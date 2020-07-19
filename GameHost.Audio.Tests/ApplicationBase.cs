using System;
using System.Collections.Generic;
using System.Reflection;
using GameHost.Applications;
using GameHost.Audio.Applications;
using GameHost.Core.Ecs;
using GameHost.Core.Execution;
using GameHost.Core.Threading;
using GameHost.Threading;
using GameHost.Threading.Apps;
using GameHost.Worlds;
using NUnit.Framework;

namespace GameHost.Audio.Tests
{
	public class ApplicationBase
	{
		public GlobalWorld Global;

		public virtual List<Type> RequiredExecutiveSystems { get; }
		public virtual List<Type> RequiredAudioSystems { get; }

		public CommonApplicationThreadListener Client;
		public CommonApplicationThreadListener Server;

		[SetUp]
		public void SetUp()
		{
			Global = new GlobalWorld();

			var systems = RequiredExecutiveSystems;
			if (systems == null)
			{
				systems = new List<Type>();

				// hack: make GameHost core systems being added to the resolver 
				var programType = typeof(GameHost.Program);
				systems.Add(programType);
				systems.Remove(programType);

				AppSystemResolver.ResolveFor<ExecutiveEntryApplication>(systems,
					t => t.GetCustomAttribute<RestrictToApplicationAttribute>()?.IsValid<ExecutiveEntryApplication>() == true);
			}

			foreach (var type in systems)
				Global.Collection.GetOrCreate(type);

			var listenerCollection = Global.World.CreateEntity();
			listenerCollection.Set<ListenerCollectionBase>(new ListenerCollection());

			systems.Clear();
			systems = RequiredAudioSystems;
			if (systems == null)
			{
				systems = new List<Type>();
				AppSystemResolver.ResolveFor<AudioApplication>(systems);
			}

			Client = new CommonApplicationThreadListener(Global, null);
			foreach (var type in systems)
			{
				Console.WriteLine(type);
				Client.Data.Collection.GetOrCreate(type);

			}

			Server = new CommonApplicationThreadListener(Global, null);
			foreach (var type in systems)
				Server.Data.Collection.GetOrCreate(type);

			var clientAppEntity = Global.World.CreateEntity();
			clientAppEntity.Set<IListener>(Client);
			clientAppEntity.Set(new PushToListenerCollection(listenerCollection));

			var serverAppEntity = Global.World.CreateEntity();
			serverAppEntity.Set<IListener>(Server);
			serverAppEntity.Set(new PushToListenerCollection(listenerCollection));

			if (Client.Scheduler is Scheduler clientScheduler)
			{
				clientScheduler.OnExceptionFound = exception =>
				{
					Console.WriteLine("Client: " + exception);
					return true;
				};
			}

			if (Server.Scheduler is Scheduler serverScheduler)
			{
				serverScheduler.OnExceptionFound = exception =>
				{
					Console.WriteLine("Server: " + exception);
					return true;
				};
			}
		}

		[TearDown]
		public void TearDown()
		{
			// TODO: Real way for disposing GlobalWorld
			Global.World.Dispose();
		}
	}
}