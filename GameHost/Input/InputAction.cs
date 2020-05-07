using DefaultEcs;

namespace GameHost.Input
{
    /// <summary>
    /// Wrapper around an entity that does possess input data.
    /// </summary>
    public struct InputAction
    {
        private Entity entity;

        public InputAction(Entity entity)
        {
            this.entity = entity;
        }

        public T GetProvider<T>()
            where T : IInputProvider
        {
            return entity.Get<T>();
        }
    }
}
