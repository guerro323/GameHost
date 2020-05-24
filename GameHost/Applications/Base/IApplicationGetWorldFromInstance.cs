using GameHost.Core.Ecs;

namespace GameHost.Applications
{
    public interface IApplicationGetWorldFromInstance
    {
        bool TryGetWorldFromInstance(Instance instance, out WorldCollection worldCollection);
    }
}
