using RevolutionSnapshot.Core.Buffers;

namespace GameHost.Inputs.Interfaces
{
	public interface IInputAction
	{
		void Serialize(ref   DataBufferWriter buffer);
		void Deserialize(ref DataBufferReader buffer);
	}
}