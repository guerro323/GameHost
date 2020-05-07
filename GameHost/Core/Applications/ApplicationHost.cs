using System;

namespace GameHost.Core.Applications
{
    /// <summary>
    /// Represent an application that host data
    /// </summary>
	public abstract class ApplicationHostBase : IDisposable
	{
		public abstract void Listen();
		public abstract void Dispose();
	}

    /// <summary>
    /// Represent an application that can receive data
    /// </summary>
    public abstract class ApplicationClientBase : IDisposable
    {
        public abstract void Connect();
        public abstract void Dispose();
    }
}
