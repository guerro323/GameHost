using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace GameHost.Core.Applications
{
    /// <summary>
    /// An interactive application 
    /// </summary>
	public class InteractiveApplicationHost : ApplicationHostBase
	{
		private PipeStream m_InPipeStream;
		private PipeStream m_OutPipeStream;
        
		private byte[] m_Buffer;

		private int m_Offset;
		private int m_Range;

        public InteractiveApplicationHost()
		{
			m_Buffer = new byte[4096];
		}

		public override void Listen()
		{
			var se = new NamedPipeServerStream("p4pipe1-server-send", PipeDirection.Out, 1);
            var cl = new NamedPipeClientStream(".", "p4pipe1-server-receive", PipeDirection.In);
            se.WaitForConnectionAsync().ContinueWith((task =>
            {
                Console.WriteLine("Server Connected!");
            }));
            cl.ConnectAsync().ContinueWith((task =>
            {
                Console.WriteLine("Client Connected!");
                m_InPipeStream.ReadAsync(m_Buffer, 0, m_Buffer.Length).ContinueWith(Callback);
            }));

            m_OutPipeStream = se;
            m_InPipeStream = cl;
        }

		public virtual void Connect()
		{
        }

        private void Callback(Task<int> task)
        {
            if (task.Result > 0)
            {
                m_Offset = 0;
                m_Range  = task.Result;
            }
            
            m_InPipeStream.ReadAsync(m_Buffer, 0, m_Buffer.Length).ContinueWith(Callback);
        }

        public virtual bool HasEvent()
        {
            if (!m_InPipeStream.IsConnected)
                return false;

            if (m_Range > 0)
            {
                return true;
            }

            return false;
        }

        public virtual Span<byte> GetData()
		{
			if (m_Range <= 0)
				throw new InvalidOperationException("Data not read.");
			var s = new Span<byte>(m_Buffer, 0, m_Range);
            m_Range = 0;
            return s;
        }

        public void SendData(ReadOnlySpan<byte> data)
        {
            m_OutPipeStream.Write(data);
            m_OutPipeStream.Flush();
        }

		public override void Dispose()
        {
            m_InPipeStream.Dispose();
			m_OutPipeStream.Dispose();
		}
	}
}
