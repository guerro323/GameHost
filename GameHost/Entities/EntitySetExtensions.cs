using DefaultEcs;

namespace GameHost.Entities
{
    public static class EntitySetExtensions
    {
        /// <summary>
        /// Destroy all entities of a given <see cref="EntitySet"/>
        /// </summary>
        /// <param name="set">The selected entity set</param>
        public static void DisposeAllEntities(this EntitySet set)
        {
            // no ref access since we do structural change
            foreach (var entity in set.GetEntities())
                entity.Dispose();
        }
    }
}
