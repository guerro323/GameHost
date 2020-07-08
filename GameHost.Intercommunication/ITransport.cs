using System;
using System.Net;

namespace GameHost.Intercommunication
{
	public enum ReceiveEvent
	{
		None,
		Connect,
		Disconnect,
		Message
	}

	public interface ITransport : IDisposable
	{
		void Connect(IPEndPoint ep);
		void Listen(int port);
		
		void Send(byte[]       data);
		bool TryReceive(out byte[] data, out ReceiveEvent type);
	}
}