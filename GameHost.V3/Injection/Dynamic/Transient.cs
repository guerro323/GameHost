namespace GameHost.V3.Injection
{
    public class Transient<T> : DynamicDependency<T>
        where T : ITransientObject, new()
    {
        public override T CreateT<TContext>(TContext context)
        {
            var result = new T();
            result.OnCreated(context);
            return result;
        }
    }

    public interface ITransientObject
    {
        void OnCreated<TContext>(TContext context)
            where TContext : IReadOnlyContext;
    }
}