using System;

public struct ParallelView
	{
		public struct Iteration : IDisposable
		{
			private bool m_Disposed;

			public int Current { get; set; }

			public void Begin()
			{
				m_Disposed = false;
			}
			
			public void Dispose()
			{
				if (m_Disposed)
					throw new InvalidOperationException();
				m_Disposed = true;
			}
			
			public bool MoveNext()
			{
				return false;
			}
		
			public Iteration GetEnumerator()
			{
				return this;
			}
		}

		public Iteration Current { get; set; }

		private int m_Index;
		private int m_Length;

		public bool MoveNext()
		{
			if (m_Index >= m_Length)
				return false;

			Current.Begin();
			
			m_Index++;
			return true;
		}
		
		public ParallelView GetEnumerator()
		{
			m_Index = 0;
			m_Length = 4;
			return this;
		}
	}