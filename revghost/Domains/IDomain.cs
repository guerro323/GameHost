using DefaultEcs;

namespace revghost.Domains;

/// <summary>
/// A domain is a sub-world that can be executed locally, in another thread; in another domain, on another process or even online
/// </summary>
public interface IDomain
{
    Entity DomainEntity { get; }
}