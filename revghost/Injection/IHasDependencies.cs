namespace revghost.Injection;

/// <summary>
/// Object that inherit this indicate that they have dependencies
/// </summary>
public interface IHasDependencies
{
    /// <summary>
    /// Current dependencies of the object
    /// </summary>
    public IDependencyCollection Dependencies { get; }
}