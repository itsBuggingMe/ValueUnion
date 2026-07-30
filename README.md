# ValueUnion

Source generator for C# 15 that automatically creates non-boxing struct unions.

```cs
using ValueUnion;

IntOrBool num = 42;

int incrementedValue = num switch
{
    // No warning!
    int i => i + 1,
    bool b => b ? 2 : 1,
};

[ValueUnion<int, bool>]
partial struct IntOrBool;
```
