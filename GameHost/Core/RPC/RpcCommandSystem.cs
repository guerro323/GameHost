using System;
using System.Collections.Generic;
using GameHost.Applications;
using GameHost.Core.Client;
using GameHost.Core.Ecs;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.RPC
{
	[RestrictToApplication(typeof(ExecutiveEntryApplication))]
	public abstract class RpcCommandSystem : AppSystem
	{
		public abstract string CommandId { get; }

		protected abstract void OnReceiveRequest(GameHostCommandResponse response);
		protected abstract void OnReceiveReply(GameHostCommandResponse   response);

		private RpcEventCollectionSystem collectionSystem;
		private StartGameHostListener    listener;
		
		private DataBufferWriter tempWriter;
		private bool             isWriting;
		private bool             isInRequestSection;

		protected RpcCommandSystem(WorldCollection collection) : base(collection)
		{
			DependencyResolver.Add(() => ref collectionSystem);
			DependencyResolver.Add(() => ref listener);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);
			collectionSystem.CommandRequest += r =>
			{
				if (CommandId.AsSpan().SequenceEqual(r.Command.Span))
				{
					OnReceiveRequest(r);
					if (isWriting)
					{
						listener.SendReply(r.Connection, r.Command, tempWriter);
						tempWriter.Length = 0;
					}
				}
			};
			collectionSystem.CommandReply += r =>
			{
				if (CommandId.AsSpan().SequenceEqual(r.Command.Span))
					OnReceiveReply(r);
			};
		}

		protected DataBufferWriter GetReplyWriter()
		{
			if (!isInRequestSection)
				throw new InvalidOperationException("Can't reply in a reply.");

			if (isWriting)
				throw new InvalidOperationException("Already writing");

			tempWriter.Length = 0;
			return tempWriter;
		}
	}
}