namespace GameHost.Simulation.TabEcs.Types
{
    public readonly struct ComponentReference
    {
        public readonly ComponentType Type;
        public readonly uint Id;

        public ComponentReference(ComponentType type, uint id)
        {
            Type = type;
            Id = id;
        }
    }
}