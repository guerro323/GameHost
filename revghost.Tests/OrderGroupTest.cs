using DefaultEcs;
using NUnit.Framework;
using revghost.Ecs;

namespace revghost.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void TestLinearNoConstraint()
    {
        using var group = new OrderGroup();
        var b1 = group.Add(null);
        var b2 = group.Add(null);

        Assert.IsTrue(group.Build());

        Assert.AreEqual(new[] {b1, b2}, group.Entities.ToArray());
    }
    
    [Test]
    public void TestLinearWithAfterConstraint()
    {
        using var group = new OrderGroup();
        var b1 = group.Add(null);
        var b2 = group.Add(b => b.After(b1));

        Assert.IsTrue(group.Build());

        Assert.AreEqual(new[] {b1, b2}, group.Entities.ToArray());
    }
    
    [Test]
    public void TestLinearWithBeforeConstraint()
    {
        using var group = new OrderGroup();
        var b1 = group.Add(null);
        var b2 = group.Add(b => b.Before(b1));

        Assert.IsTrue(group.Build());

        Assert.AreEqual(new[] {b2, b1}, group.Entities.ToArray());
    }
    
    [Test]
    public void TestReverseLinearWithAfterConstraint()
    {
        using var group = new OrderGroup();
        
        Entity b1 = default, b2 = default;
        
        b1 = group.Add(b => b.After(b2));
        b2 = group.Add(null);

        Assert.IsTrue(group.Build());

        Assert.AreEqual(new[] {b2, b1}, group.Entities.ToArray());
    }
    
    [Test]
    public void TestReverseLinearWithBeforeConstraint()
    {
        using var group = new OrderGroup();
        
        Entity b1 = default, b2 = default;
        
        b1 = group.Add(b => b.Before(b2));
        b2 = group.Add(null);

        Assert.IsTrue(group.Build());

        Assert.AreEqual(new[] {b1, b2}, group.Entities.ToArray());
    }

    [Test]
    public void TestGroup()
    {
        using var group = new OrderGroup();

        Entity begin, end = default, b1;
        
        begin = group.Add(b => b.Before(end));
        end = group.Add(b => b.After(begin));

        b1 = group.Add(b =>
        {
            b
                .After(begin)
                .Before(end);
        });
        
        Assert.IsTrue(group.Build());

        Assert.AreEqual(new[] {begin, b1, end}, group.Entities.ToArray());
    }

    [Test]
    public void TestCircular()
    {
        using var group = new OrderGroup();

        Entity b1, b2 = default;
        
        b1 = group.Add(b => b.After(b2));
        b2 = group.Add(b => b.After(b1));
        
        Assert.IsFalse(group.Build());
    }

    [Test]
    public void TestType()
    {
        using var group = new OrderGroup();
        
        var a = group.Add(b => b.After(typeof(B)));
        a.Set(typeof(A));
        
        var b = group.Add(_ => {});
        b.Set(typeof(B));

        group.Build();
        
        Assert.AreEqual(new[] {b, a}, group.Entities.ToArray());
    }

    public class A
    {
    }

    public class B
    {
    }
}