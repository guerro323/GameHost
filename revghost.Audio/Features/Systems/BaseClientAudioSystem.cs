using revghost;
using revghost.Ecs;
using revghost.Injection;
using revghost.Injection.Dependencies;

namespace GameHost.Audio.Features.Systems;

public abstract class BaseClientAudioSystem : AppSystem
{
    protected AudioClientFeature Client;
    
    protected BaseClientAudioSystem(Scope scope) : base(scope)
    {
        Dependencies.AddRef(() => ref Client);
    }
}