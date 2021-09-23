namespace GameHost.V3.Injection
{
    /// <summary>
    /// Provide a dependency factory
    /// </summary>
    public abstract class DynamicDependency
    {
        public abstract object Create<TContext>(TContext context)
            where TContext : IReadOnlyContext;
    }

    /// <inheritdoc cref="DynamicDependency"/>
    public abstract class DynamicDependency<T> : DynamicDependency
    {
        public abstract T CreateT<TContext>(TContext context)
            where TContext : IReadOnlyContext;

        public override object Create<TContext>(TContext context)
        {
            return CreateT(context);
        }
    }
}