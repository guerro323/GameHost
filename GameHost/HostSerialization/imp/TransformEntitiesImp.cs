using System.Collections.Generic;
using DefaultEcs;
using RevolutionSnapshot.Core;

namespace GameHost.HostSerialization.imp
{
    public class TransformEntitiesImp<T> : ImpBase<T>, IGetEntityMap
        where T : ITransformEntities<T>
    {
        public override void OnImp(ref T data)
        {
            var copy = data;
            copy.TransformFrom(SourceToConvertedMap, data);
            data = copy;
        }

        public TwoWayDictionary<Entity, Entity> SourceToConvertedMap { get; set; }
    }

    public static class TransformEntitiesImpExtension
    {
        public static ComponentOperationBase<T> AddTransformEntities<T>(this ComponentOperationBase<T> op)
            where T : ITransformEntities<T>
        {
            return op.AddImp(new TransformEntitiesImp<T>());
        }
    }

    public interface ITransformEntities<in T>
    {
        void TransformFrom(TwoWayDictionary<Entity, Entity> fromToMap, T other);
    }
}
