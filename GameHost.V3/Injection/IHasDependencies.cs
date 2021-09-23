namespace GameHost.V3.Injection
{
    public interface IHasDependencies
    {
        public IDependencyCollection Dependencies { get; }
    }
}