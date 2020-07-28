using System;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using GameHost.Core.IO;
using GameHost.Native.Char;
using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Core.RPC
{
	public ref struct GameHostCommandResponse
	{
		public TransportConnection Connection;
		public CharBuffer128        Command;
		public DataBufferReader    Data;

		public static GameHostCommandResponse GetResponse(TransportConnection connection, DataBufferReader data)
		{
			return new GameHostCommandResponse
			{
				Connection = connection,
				Command    = data.ReadBuffer<CharBuffer128>(),
				Data       = new DataBufferReader(data, data.CurrReadIndex, data.Length)
			};
		}

		public unsafe T Deserialize<T>(JsonSerializerOptions options = null)
		{
			return JsonSerializer.Deserialize<T>(Data.ReadString(), options);
		}
	}
}