using System;
using System.Diagnostics;

namespace GameHost.Core.Client
{
	public class GameClient
	{
		public int  ProcessId;
		public bool IsHwndIntegrated;
		public bool IsHwndInFront;

		public IntPtr Hwnd;

		public Process Process => Process.GetProcessById(ProcessId);
	}
}