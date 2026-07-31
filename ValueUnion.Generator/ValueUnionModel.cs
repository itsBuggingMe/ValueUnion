namespace ValueUnion.Generator;

internal record struct ValueUnionModel(
    int Arity,
    string? Namespace,
    string Name,
    string FullName,
    bool IsNullValid,
    int DefaultTypeIndex,
    EquatableArray<TypeDeclarationModel> TypeDeclarations,
    EquatableArray<TypeModel> Types);
