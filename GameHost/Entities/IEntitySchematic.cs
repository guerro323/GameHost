using DefaultEcs;
using GameHost.Core.Ecs;

namespace GameHost.Entities
{
    public interface IEntitySchematic
    {
        void Init(WorldCollection collection);
        void Apply(Entity entity);
    }

    public class EntitySchematicSystem : AppSystem
    {
        public EntitySchematicSystem(WorldCollection collection) : base(collection)
        {
        }
    }
}
