namespace Compositions.NET.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }
    /// <summary>
    /// Make sure that components are pooled
    /// </summary>
    [Test]
    public void PoolingTest()
    {
        var c1 = new BaseComposition();
        c1.SetSome(1);
        var first = c1.Some;
        c1.SetSome(2);
        Assert.That(first.X , Is.EqualTo(0));
        Assert.That(c1.Some.X , Is.EqualTo(2));
        c1.SetSomeOther(1);
        var second = c1.SomeOther;
        c1.SetSomeOther(2);
        Assert.That(second, Is.Not.EqualTo(c1.SomeOther));
        Assert.That(c1.SomeOther.X,Is.EqualTo(2));
        Assert.That(second.X, Is.EqualTo(1));
    }

    [Test]
    public void InstanceSetPoolingTest()
    {
        var comp = new BaseComposition();
        var component = new SomeComponent();
        component.X = 2;
        comp.Some = component;
        comp.SetSome(1);
        Assert.That(component.X, Is.EqualTo(2));
        var current = comp.Some;
        comp.Some = component;
        Assert.That(current.X, Is.EqualTo(0));
        Assert.That(component.X, Is.EqualTo(2));
    }
}

public partial class BaseComposition: IComposition
{
    
}
[IncludedInComposition(typeof(BaseComposition))]
public partial class SomeComponent : IComponent,IClearable
{
    public int X;
}
[IncludedInComposition(typeof(BaseComposition))]

public partial class SomeOtherComponent : IComponent
{
    public int X;
}