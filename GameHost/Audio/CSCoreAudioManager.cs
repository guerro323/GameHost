using System;
using System.Collections.Generic;
using CSCore;
using CSCore.SoundOut;

namespace GameHost.Sounds
{
	public class CSCoreAudioManager : IDisposable
	{
		private class Element
		{
			public WasapiOut   Out;
			public IWaveSource Source;
		}

		private List<Element> audioElements;

		public CSCoreAudioManager()
		{
			audioElements = new List<Element>();
		}
		
		public void Dispose()
		{
			
		}

		public void Play()
		{
			
		}
	}
}
