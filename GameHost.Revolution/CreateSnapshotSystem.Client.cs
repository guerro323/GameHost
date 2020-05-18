using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GameHost.Revolution
{
	public partial class CreateSnapshostSystem
	{
		private class ClientObject : IDisposable
		{
			public Task   RunningTask;
			public byte[] WrittenData;
			public int    DataLength;

			public bool IsRunning => !RunningTask.IsCompleted;

			public ClientObject()
			{
				RunningTask = new Task(RunSerializer);
			}

			public void RunSerializer()
			{
				DataLength = 0;
			}

			public void Dispose()
			{
				RunningTask?.Dispose();
				Array.Clear(WrittenData, 0, WrittenData.Length);
			}
		}

		private Dictionary<SerializationClient, ClientObject> clientObjects = new Dictionary<SerializationClient, ClientObject>();

		public void SerializeFor(ReadOnlySpan<SerializationClient> clients)
		{
			foreach (var client in clients)
			{
				var obj = clientObjects[client];
				// Clear client data first
				Array.Clear(obj.WrittenData, 0, obj.WrittenData.Length);
				Array.Resize(ref obj.WrittenData, Math.Max(obj.WrittenData.Length, 1024));
				// Start client task
				obj.RunningTask.Start();
			}

			foreach (var client in clients)
			{
				clientObjects[client].RunningTask.Wait();
			}
		}

		public Span<byte> GetDataOf(SerializationClient client)
		{
			var obj = clientObjects[client];
			return new Span<byte>(obj.WrittenData, 0, obj.DataLength);
		}

		public SerializationClient CreateClient()
		{
			var client = new SerializationClient(clientObjects.Count);
			clientObjects[client] = new ClientObject();
			return client;
		}

		public void DestroyClient(SerializationClient client)
		{
			clientObjects[client].Dispose();
			clientObjects.Remove(client);
		}
	}
}