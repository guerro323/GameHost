using System;

namespace GameHost.Native.Char
{
	public interface ICharBuffer
	{
		int        Capacity { get; }
		int        Length   { get; set; }
		Span<char> Span     { get; }
	}
}