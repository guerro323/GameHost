using GameHost.Audio.Features;
using GameHost.Core.IO;

namespace GameHost.Audio;

public class SoLoudBackendFeature : IAudioBackendFeature
{
    public SoLoudBackendFeature(TransportDriver driver)
    {
        Driver = driver;
        TransportAddress = driver.TransportAddress;
    }

    public TransportDriver Driver { get; }
    public TransportAddress TransportAddress { get; }
}