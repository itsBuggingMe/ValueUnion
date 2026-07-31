namespace ValueUnion.Tests;

public class GeneratorShapeTests
{
    [Test]
    public void SameNamesInDifferentNamespacesAreGenerated()
    {
        First.SameNameUnion first = 42;
        Second.SameNameUnion second = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first.TryGetValue(out int firstValue), Is.True);
            Assert.That(firstValue, Is.EqualTo(42));
            Assert.That(second.TryGetValue(out bool secondValue), Is.True);
            Assert.That(secondValue, Is.True);
        }
    }

    [Test]
    public void NestedUnionIsGeneratedInItsContainingType()
    {
        NestedContainer.NestedUnion value = 42;

        Assert.That(value.TryGetValue(out int number), Is.True);
        Assert.That(number, Is.EqualTo(42));
    }

    [Test]
    public void GenericUnionPreservesItsTypeParameters()
    {
        GenericUnion<string> value = true;

        Assert.That(value.TryGetValue(out bool flag), Is.True);
        Assert.That(flag, Is.True);
    }

    [Test]
    public void UnionInGenericContainingTypeDoesNotOverlapFields()
    {
        GenericContainer<string>.NestedUnion value = 42;

        Assert.That(value.TryGetValue(out int number), Is.True);
        Assert.That(number, Is.EqualTo(42));
    }

    [Test]
    public void DeclarationModifiersArePreserved()
    {
        RecordUnion record = true;
        ReadOnlyUnion readOnly = 42;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(record.TryGetValue(out bool flag), Is.True);
            Assert.That(flag, Is.True);
            Assert.That(readOnly.TryGetValue(out int number), Is.True);
            Assert.That(number, Is.EqualTo(42));
        }
    }

    [Test]
    public void ReferenceAlternativesDoNotProduceNullableWarnings()
    {
        ReferenceUnion text = "value";
        ReferenceUnion number = 42;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(text.TryGetValue(out string? textValue), Is.True);
            Assert.That(textValue, Is.EqualTo("value"));
            Assert.That(number.TryGetValue(out int numberValue), Is.True);
            Assert.That(numberValue, Is.EqualTo(42));
        }
    }

    [Test]
    public void NullConstructorParametersLeaveTheUnionInactive()
    {
        ReferenceUnion nullText = new((string?)null);
        ReferenceUnion nullNumber = new((int?)null);
        NullableValueUnion nullNullableValue = new((int?)null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(nullText.HasValue, Is.False);
            Assert.That(nullNumber.HasValue, Is.False);
            Assert.That(nullNullableValue.HasValue, Is.False);
            Assert.That(nullText.TryGetValue(out string? textValue), Is.False);
            Assert.That(textValue, Is.Null);
            Assert.That(nullNumber.TryGetValue(out int numberValue), Is.False);
            Assert.That(numberValue, Is.Zero);
            Assert.That(nullNullableValue.TryGetValue(out int? nullableValue), Is.False);
            Assert.That(nullableValue, Is.Null);
        }
    }

    [Test]
    public void DefaultOptionSelectsTheDefaultUnionCase()
    {
        DefaultBoolUnion defaultValue = default;
        DefaultBoolUnion number = 42;
        DefaultBoolUnion flag = true;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(defaultValue.TryGetValue(out bool defaultFlag), Is.True);
            Assert.That(defaultFlag, Is.False);
            Assert.That(defaultValue.TryGetValue(out int _), Is.False);
            Assert.That(defaultValue.Value, Is.EqualTo(false));
            Assert.That(number.TryGetValue(out int numberValue), Is.True);
            Assert.That(numberValue, Is.EqualTo(42));
            Assert.That(flag.TryGetValue(out bool flagValue), Is.True);
            Assert.That(flagValue, Is.True);
            Assert.That(typeof(DefaultBoolUnion).GetProperty("HasValue"), Is.Null);
        }
    }

    [Test]
    public void IdenticalAlternativesAreNotGenerated()
    {
        Assert.That(
            typeof(DuplicateUnion).GetInterfaces(),
            Does.Not.Contain(typeof(global::System.Runtime.CompilerServices.IUnion)));
    }
}

partial class NestedContainer
{
    [Union<int, bool>]
    public partial struct NestedUnion;
}

partial class GenericContainer<T>
{
    [Union<int, T>]
    public partial struct NestedUnion;
}

[Union<int, bool>]
partial record struct RecordUnion;

[Union<int, bool>]
readonly partial struct ReadOnlyUnion;

[Union<string, int>]
partial struct ReferenceUnion;

[Union<int?, bool>]
partial struct NullableValueUnion;

[Union<int, bool>(Default = typeof(bool))]
partial struct DefaultBoolUnion;

[Union<int, bool>]
partial struct GenericUnion<T>
    where T : class;

[Union<int, int>]
partial struct DuplicateUnion;
