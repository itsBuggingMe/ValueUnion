# ValueUnion

C# 15 has finally added discrimminated unions! However, using a struct as a union case type results in boxing, with manual implementations requiring a lot of [boilerplate](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/union#custom-union-types).

This library autogenerates that boilerplate so it's easier to use unions without allocating or errors. Simply add the `[Union<>]` attribute and mark your union struct as partial.

```cs
using ValueUnion;

CustomerId id = Guid.NewGuid();

string formatted = id switch
{
    int value => $"legacy:{value}",
    Guid value => $"modern:{value:N}"
};

[Union<int, Guid>]
partial struct CustomerId;
```

<details>
  <summary>Source generated code</summary>

```cs
[Union]
[StructLayout(LayoutKind.Auto)]
partial struct CustomerId : IUnion
{
    private readonly byte _tag;
    private readonly Impl _inner;
    
    [StructLayout(LayoutKind.Explicit)]
    private struct Impl
    {
        [FieldOffset(0)]
        internal int Value1;
        [FieldOffset(0)]
        internal Guid Value2;
    }
    
    public CustomerId(int? value)
    {
        if (value is int v)
        {
            _inner.Value1 = v;
            _tag = 1;
        }
    }

    public CustomerId(Guid? value)
    {
        if (value is Guid v)
        {
            _inner.Value2 = v;
            _tag = 2;
        }
    }
    
    public readonly bool HasValue => _tag != 0;
    
    public readonly object? Value => _tag switch
    {
        1 => _inner.Value1,
        2 => _inner.Value2,
        _ => null,
    };
    
    public readonly bool TryGetValue(out int value)
    {
        value = _inner.Value1;
        return _tag == 1;
    }

    public readonly bool TryGetValue(out Guid value)
    {
        value = _inner.Value2;
        return _tag == 2;
    }
}
```
</details>

## Installation

```console
dotnet add package ValueUnion
```

## Usage

Simply apply the `[Union<>]` attribute to a partial struct declaration. You can add up to 8 generic parameters for up to 8 possible types within a union. Types can be struct, primitives, classes, anything e.g. `[Union<double, string, int, long>]`. When all union cases are unmanaged, the union's cases are overlapped to save memory (this cannot be done if case types are managed due to the GC).

By default, a union also has a `null` case. Using the union example above, a `default(CustomerId)` has no active case and may warn if the null case is unhandled. You can get around this by setting a type to use in the default case with the `Default` property. If `[Union<int, Guid>(Default = typeof(int))]` was used instead, a `default(CustomerId)` would have case `int` with value `0`.

```cs
CustomerId defaultId = default;

Debug.Assert(defaultId switch
{
    // case is int, as it is default case
    int => true,
    Guid => false,
});

[Union<int, Guid>(Default = typeof(int))]
partial struct CustomerId;
```