using Microsoft.CodeAnalysis;
namespace ValueUnion.Generator;

[Generator(LanguageNames.CSharp)]
public class ValueUnionGenerator : IIncrementalGenerator
{
    public static SymbolDisplayFormat GlobalFullyQualifiedTypeNameFormat { get; } = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included);
    public static SymbolDisplayFormat FullyQualifiedTypeNameFormat { get; } = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);
    private record struct SourceOutput(string Hintname, string SourceText);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource("UnionAttribute.g.cs", UnionAttributeSource));
        for (int i = 2; i <= 8; i++)
            RegisterForArity(i, context);
    }

    private static void RegisterForArity(int arity, IncrementalGeneratorInitializationContext ctx)
    {
        if (arity <= 0)
            throw new ArgumentOutOfRangeException(nameof(arity));

        IncrementalValuesProvider<SourceOutput> source = 
            ctx.SyntaxProvider.ForAttributeWithMetadataName($"ValueUnion.UnionAttribute`{arity}", (n, ct) => true, CreateModel)
            .Where(s => s != default)
            .Select(CreateSource);

        ctx.RegisterSourceOutput(source, (ctx, source) => ctx.AddSource(source.Hintname, source.SourceText));
    }

    private static ValueUnionModel CreateModel(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return default;

        if (ctx.Attributes.Length == 0 || ctx.Attributes[0].AttributeClass is not { } attributeClass)
            return default;

        AttributeData attribute = ctx.Attributes[0];
        var typeArgs = attributeClass.TypeArguments;

        if(typeArgs.Length == 0)
            return default;

        ITypeSymbol? defaultType = null;
        foreach (KeyValuePair<string, TypedConstant> namedArgument in attribute.NamedArguments)
        {
            if (namedArgument.Key == "Default")
            {
                defaultType = namedArgument.Value.Value as ITypeSymbol;
                break;
            }
        }

        int defaultTypeIndex = -1;
        if (defaultType is not null)
        {
            for (int i = 0; i < typeArgs.Length; i++)
            {
                if (SymbolEqualityComparer.Default.Equals(typeArgs[i], defaultType))
                {
                    defaultTypeIndex = i;
                    break;
                }
            }

            if (defaultTypeIndex < 0)
                return default;
        }

        for (int i = 0; i < typeArgs.Length; i++)
        {
            for (int j = i + 1; j < typeArgs.Length; j++)
            {
                if (SymbolEqualityComparer.Default.Equals(typeArgs[i], typeArgs[j]))
                    return default;
            }
        }

        TypeModel[] arr = new TypeModel[typeArgs.Length];
        bool overlapFields = true;

        for (int i = 0; i < typeArgs.Length; i++)
        {
            ITypeSymbol typeArgument = typeArgs[i];
            string fullName = typeArgument.ToDisplayString(GlobalFullyQualifiedTypeNameFormat);
            bool isNullableValueType = typeArgument is INamedTypeSymbol
            {
                OriginalDefinition.SpecialType: SpecialType.System_Nullable_T,
                TypeArguments.Length: 1,
            };

            arr[i] = new TypeModel(
                fullName,
                isNullableValueType ? fullName : $"{fullName}?",
                isNullableValueType
                    ? ((INamedTypeSymbol)typeArgument).TypeArguments[0].ToDisplayString(GlobalFullyQualifiedTypeNameFormat)
                    : fullName);
            overlapFields &= typeArgument.IsUnmanagedType;
        }

        Stack<INamedTypeSymbol> containingTypes = new();
        for (INamedTypeSymbol? current = typeSymbol; current is not null; current = current.ContainingType)
        {
            overlapFields &= current.Arity == 0;
            containingTypes.Push(current);
        }

        TypeDeclarationModel[] declarations = new TypeDeclarationModel[containingTypes.Count];
        int declarationIndex = 0;
        while (containingTypes.Count > 0)
            declarations[declarationIndex++] = CreateTypeDeclarationModel(containingTypes.Pop());

        return new ValueUnionModel(
            typeArgs.Length, 
            typeSymbol.ContainingNamespace is { IsGlobalNamespace: true } ? null : typeSymbol.ContainingNamespace.ToString(),
            typeSymbol.Name,
            typeSymbol.ToDisplayString(FullyQualifiedTypeNameFormat),
            defaultType is null,
            defaultTypeIndex,
            overlapFields,
            new(declarations),
            new(arr));
    }

    private static SourceOutput CreateSource(ValueUnionModel model, CancellationToken ct)
    {
        CodeBuilder codeBuilder = CodeBuilder
            .ThreadShared
            .AppendLine("#nullable enable")
            .If(model.Namespace is not null, model.Namespace, (n, c) => c.Append("namespace ").Append(n).AppendLine(";"))
            .AppendLine()
            .Foreach<TypeDeclarationModel>(model.TypeDeclarations, ct, (in declaration, cb, i) => cb.If(i == model.TypeDeclarations.Length - 1, cb => cb
                .AppendLine("[global::System.Runtime.CompilerServices.Union]")
                .AppendLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]"))
                .If((declaration.Modifiers & TypeDeclarationModifiers.ReadOnly) != 0, cb => cb.Append("readonly "))
                .If((declaration.Modifiers & TypeDeclarationModifiers.Ref) != 0, cb => cb.Append("ref "))
                .Append("partial ")
                .If((declaration.Modifiers & TypeDeclarationModifiers.Record) != 0, cb => cb.Append("record "))
                .If((declaration.Modifiers & TypeDeclarationModifiers.Class) != 0, cb => cb.Append("class "))
                .If((declaration.Modifiers & TypeDeclarationModifiers.Struct) != 0, cb => cb.Append("struct "))
                .If((declaration.Modifiers & TypeDeclarationModifiers.Interface) != 0, cb => cb.Append("interface "))
                .Append(declaration.Name)
                .If(i == model.TypeDeclarations.Length - 1, cb => cb.Append(" : global::System.Runtime.CompilerServices.IUnion"))
                .AppendLine()
                .Scope())
                // fields
                .AppendLine("private readonly byte _tag;")
                .If(model.OverlapFields, cb => cb
                    .AppendLine("private readonly Impl _inner;")
                    .AppendLine()
                    .AppendLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Explicit)]")
                    .AppendLine("private struct Impl").Scope())
                .AppendLine("#nullable disable")
                .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                    cb
                    .If(model.OverlapFields, cb => cb.AppendLine("[global::System.Runtime.InteropServices.FieldOffset(0)]"))
                    .Append(!model.OverlapFields ? "private readonly " : "internal ")
                    .Append(type.FullName).Append(model.OverlapFields ? " Value" : " _value")
                    .Append(i + 1)
                    .AppendLine(";"))
                .AppendLine("#nullable enable")
                .If(model.OverlapFields, cb => cb.Unscope())
                .AppendLine()
                // ctors
                .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                    AppendConstructor(cb, model, in type, i))
                .AppendLine()
                .If(model.IsNullValid, cb => cb
                    .AppendLine("public readonly bool HasValue => _tag != 0;")
                    .AppendLine())
                .AppendLine("public readonly object? Value => _tag switch")
                .Scope()
                    // Value
                    .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                        cb.Append(GetTag(model, i)).Append(" => ").Append(model.OverlapFields ? "_inner.Value" : "_value").Append(i + 1).AppendLine("!,"))
                    .AppendLine("_ => null,")
                .Unscope()
                .AppendLine(";")
                .AppendLine()
                // TryGet
                .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                cb.Append("public readonly bool TryGetValue(out ").Append(type.FullName).AppendLine(" value)")
                    .Scope()
                        .Append("value = ")
                        .Append(model.OverlapFields ? "_inner.Value" : "_value").Append(i + 1)
                        .AppendLine(";")
                        .Append("return _tag == ").Append(GetTag(model, i)).AppendLine(";")
                    .Unscope())
            .Foreach<TypeDeclarationModel>(model.TypeDeclarations, ct, (in declaration, cb, i) => cb.Unscope());

        return new SourceOutput($"{model.FullName.Replace('<', '_').Replace('>', '_')}.g.cs", codeBuilder.ToString());
    }

    private static void AppendConstructor(
        CodeBuilder codeBuilder,
        ValueUnionModel model,
        in TypeModel type,
        int index)
    {
        int tag = GetTag(model, index);
        string parameterType = model.IsNullValid ? type.NullableFullName : type.FullName;

        codeBuilder
            .Append("public ").Append(model.Name).Append("(").Append(parameterType).AppendLine(" value)")
            .Scope();

        if (model.IsNullValid)
        {
            codeBuilder
                .Append("if (value is ").Append(type.PatternTypeName).AppendLine(" v)")
                .Scope()
                    .Append(model.OverlapFields ? "_inner.Value" : "_value").Append(index + 1).AppendLine(" = v;")
                    .Append("_tag = ").Append(tag).AppendLine(";")
                .Unscope();
        }
        else
        {
            codeBuilder
                .Append(model.OverlapFields ? "_inner.Value" : "_value").Append(index + 1).AppendLine(" = value;")
                .Append("_tag = ").Append(tag).AppendLine(";");
        }

        codeBuilder.Unscope();
    }

    private static int GetTag(ValueUnionModel model, int typeIndex)
        => typeIndex == model.DefaultTypeIndex ? 0 : typeIndex + 1;

    private static TypeDeclarationModel CreateTypeDeclarationModel(INamedTypeSymbol typeSymbol)
    {
        TypeDeclarationModifiers modifiers = typeSymbol.TypeKind switch
        {
            TypeKind.Class => TypeDeclarationModifiers.Class,
            TypeKind.Struct => TypeDeclarationModifiers.Struct,
            TypeKind.Interface => TypeDeclarationModifiers.Interface,
            _ => TypeDeclarationModifiers.None,
        };

        if (typeSymbol.IsRecord)
            modifiers |= TypeDeclarationModifiers.Record;
        if (typeSymbol.IsReadOnly)
            modifiers |= TypeDeclarationModifiers.ReadOnly;
        if (typeSymbol.IsRefLikeType)
            modifiers |= TypeDeclarationModifiers.Ref;

        string name = typeSymbol.Name;
        if (typeSymbol.TypeParameters.Length > 0)
        {
            name += "<";
            name += string.Join(", ", typeSymbol.TypeParameters.Select(p => p.Name));
            name += ">";
        }

        return new TypeDeclarationModel(name, modifiers);
    }

    private const string UnionAttributeSource =
        """
        #nullable enable
        namespace ValueUnion
        {
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute<T1, T2> : global::System.Attribute
            {
                public global::System.Type? Default { get; set; }
            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute<T1, T2, T3> : global::System.Attribute
            {
                public global::System.Type? Default { get; set; }
            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute<T1, T2, T3, T4> : global::System.Attribute
            {
                public global::System.Type? Default { get; set; }
            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute<T1, T2, T3, T4, T5> : global::System.Attribute
            {
                public global::System.Type? Default { get; set; }
            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute<T1, T2, T3, T4, T5, T6> : global::System.Attribute
            {
                public global::System.Type? Default { get; set; }
            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute<T1, T2, T3, T4, T5, T6, T7> : global::System.Attribute
            {
                public global::System.Type? Default { get; set; }
            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class UnionAttribute<T1, T2, T3, T4, T5, T6, T7, T8> : global::System.Attribute
            {
                public global::System.Type? Default { get; set; }
            }
        }
        """;
}
