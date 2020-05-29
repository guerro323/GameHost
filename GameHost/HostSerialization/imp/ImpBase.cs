using DefaultEcs;
using RevolutionSnapshot.Core;

namespace GameHost.HostSerialization.imp
{
    public abstract class ImpBase {}
    
    // the name is actually a short version of implementation^^
    
    /// <summary>
    /// The imp is a shortcut to a very short operation on data that should operate before component operations <see cref="ComponentOperationBase{T}.OnUpdate"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ImpBase<T> : ImpBase
    {
        public abstract void OnImp(ref T data);
    }

    public interface IGetEntityMap
    {
        public TwoWayDictionary<Entity, Entity> SourceToConvertedMap { get; set; }
    }
}
