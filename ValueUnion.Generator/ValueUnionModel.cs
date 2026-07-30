namespace ValueUnion.Generator;

internal record struct ValueUnionModel(int Arity, string? Namespace, string Name, string FullName, EquatableArray<TypeModel> Types);