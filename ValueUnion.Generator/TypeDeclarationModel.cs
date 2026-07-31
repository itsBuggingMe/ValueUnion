namespace ValueUnion.Generator;

[Flags]
internal enum TypeDeclarationModifiers
{
    None = 0,
    Class = 1 << 0,
    Struct = 1 << 1,
    Interface = 1 << 2,
    Record = 1 << 3,
    ReadOnly = 1 << 4,
    Ref = 1 << 5,
}

internal record struct TypeDeclarationModel(string Name, TypeDeclarationModifiers Modifiers);
