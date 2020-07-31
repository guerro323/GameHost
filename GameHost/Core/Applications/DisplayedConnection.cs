using System;
using System.Net;

namespace GameHost.Applications
{
	public struct DisplayedConnection
	{
		public Type   ApplicationType;
		public string Type;
		public string Name;

		public IPEndPoint EndPoint;
	}
}