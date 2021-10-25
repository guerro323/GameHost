namespace revghost.Injection.Dynamic;

/// <summary>
/// Provide a dependency factory
/// </summary>
public abstract class DynamicDependency
{
    /// <summary>
    /// Create an object with context
    /// </summary>
    /// <returns>The newly created object</returns>
    public abstract object Create<TContext>(TContext context)
        where TContext : IReadOnlyContext;
}

/// <inheritdoc cref="DynamicDependency"/>
public abstract class DynamicDependency<T> : DynamicDependency
{
    /// <summary>
    /// Create an object with context
    /// </summary>
    /// <returns>The newly created object</returns>
    public abstract T CreateT<TContext>(TContext context)
        where TContext : IReadOnlyContext;

    public override object Create<TContext>(TContext context)
    {
        return CreateT(context);
    }
}