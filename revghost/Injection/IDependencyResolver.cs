namespace revghost.Injection;

public interface IDependencyResolver
{
    /// <summary>
    /// Queue a <see cref="DependencyCollection"/> for resolving it later.
    /// </summary>
    /// <param name="collection">The dependency collection</param>
    public void Queue(IDependencyCollection collection);
    /// <summary>
    /// Dequeue the <see cref="DependencyCollection"/>. It will not get resolved later.
    /// </summary>
    /// <param name="dependencyCollection">The dependency collection</param>
    void Dequeue(IDependencyCollection dependencyCollection);
}