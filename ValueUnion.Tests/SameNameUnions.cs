namespace ValueUnion.Tests.First
{
    [Union<int, bool>]
    partial struct SameNameUnion;
}

namespace ValueUnion.Tests.Second
{
    [Union<float, bool>]
    partial struct SameNameUnion;
}
