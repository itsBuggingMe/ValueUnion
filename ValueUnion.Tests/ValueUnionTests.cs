namespace ValueUnion.Tests;

public class Tests
{
    [Test]
    public void SwitchSyntaxCanBeUsed()
    {
        TestUnion x = 42;

        Assert.That(x switch
        {
            float y => throw new Exception(),
            int z => z,
        }, Is.EqualTo(42));
    }

    [Test]
    public void IntValueCanBeRead()
    {
        TestUnion x = 42;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(x.Value, Is.EqualTo(42));
            Assert.That(x.TryGetValue(out int value), Is.True);
            Assert.That(value, Is.EqualTo(42));
            Assert.That(x.TryGetValue(out float _), Is.False);
        }
    }

    [Test]
    public void FloatValueCanBeRead()
    {
        TestUnion x = 3.5f;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(x.Value, Is.EqualTo(3.5f));
            Assert.That(x.TryGetValue(out float value), Is.True);
            Assert.That(value, Is.EqualTo(3.5f));
            Assert.That(x.TryGetValue(out int _), Is.False);
        }
    }

    [Test]
    public void DefaultValueHasNoActiveCase()
    {
        TestUnion x = default;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(x.Value, Is.Null);
            Assert.That(x.TryGetValue(out int _), Is.False);
            Assert.That(x.TryGetValue(out float _), Is.False);
        }
    }
}

[ValueUnion<int, float>]
partial struct TestUnion;

[ValueUnion<int, bool>]
partial struct IntOrBool;