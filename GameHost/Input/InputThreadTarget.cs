using DefaultEcs;

namespace GameHost.Input
{
    /// <summary>
    /// Wrapper around an entity that does possess input data.
    /// </summary>
    public struct InputThreadTarget
    {
        public Entity Target;
    }

    public struct ThreadInputToCompute
    {
        public Entity Source;
    }
}
