namespace ValueUnion.Generator;

internal record struct ValueUnionModel(
    int Arity,
    string? Namespace,
    string Name,
    string FullName,
    bool IsNullValid,
    int DefaultTypeIndex,
    bool OverlapFields,
    EquatableArray<TypeDeclarationModel> TypeDeclarations,
    EquatableArray<TypeModel> Types);
