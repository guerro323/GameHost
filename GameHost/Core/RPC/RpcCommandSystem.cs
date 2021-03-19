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

		private RpcLowLevelSystem collectionSystem;
		private StartGameHostListener    listener;

		private DataBufferWriter tempWriter;
		private bool             isWriting;
		private bool             isInRequestSection;

		protected RpcCommandSystem(WorldCollection collection) : base(collection)
		{
			tempWriter = new DataBufferWriter(0);
			
			DependencyResolver.Add(() => ref collectionSystem);
			DependencyResolver.Add(() => ref listener);
		}

		protected override void OnDependenciesResolved(IEnumerable<object> dependencies)
		{
			base.OnDependenciesResolved(dependencies);
			collectionSystem.Events.CommandRequest += r =>
			{
				if (CommandId.AsSpan().SequenceEqual(r.Command.Span))
				{
					isInRequestSection = true;
					OnReceiveRequest(r);
					if (isWriting)
					{
						isWriting = false;
						listener.SendReply(r.Connection, r.Command, tempWriter);
						tempWriter.Length = 0;
					}
				}
			};
			collectionSystem.Events.CommandReply += r =>
			{
				isInRequestSection = false;
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
			isWriting         = true;
			return tempWriter;
		}
	}
}