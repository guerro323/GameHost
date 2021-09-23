namespace GameHost.V3.Injection
{
    public interface IDependencyResolver
    {
        public void Queue(IDependencyCollection collection);
        void Dequeue(IDependencyCollection dependencyCollection);
    }
}