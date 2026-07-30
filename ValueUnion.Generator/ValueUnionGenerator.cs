using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Diagnostics;

namespace ValueUnion.Generator;

[Generator(LanguageNames.CSharp)]
public class ValueUnionGenerator : IIncrementalGenerator
{
    public static SymbolDisplayFormat TypeNameFormat { get; } = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

    public static SymbolDisplayFormat FullyQualifiedTypeNameFormat { get; } =new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included);

    private record struct SourceOutput(string Hintname, string SourceText);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(static ctx => ctx.AddSource("ValueUnionAttribute.g.cs", ValueUnionAttributeSource));
        for (int i = 2; i <= 8; i++)
            RegisterForArity(i, context);
    }

    private static void RegisterForArity(int arity, IncrementalGeneratorInitializationContext ctx)
    {
        if (arity <= 0)
            throw new ArgumentOutOfRangeException(nameof(arity));

        IncrementalValuesProvider<SourceOutput> source = 
            ctx.SyntaxProvider.ForAttributeWithMetadataName($"ValueUnion.ValueUnionAttribute`{arity}", (n, ct) => true, CreateModel)
            .Where(s => s != default)
            .Select(CreateSource);

        ctx.RegisterSourceOutput(source, (ctx, source) => ctx.AddSource(source.Hintname, source.SourceText));
    }

    private static ValueUnionModel CreateModel(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        if (ctx.TargetSymbol is not INamedTypeSymbol typeSymbol)
            return default;

        ImmutableArray<ITypeSymbol> typeArgs = typeSymbol
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass is
            {
                Name: "ValueUnionAttribute",
                ContainingNamespace:
                {
                    Name: "ValueUnion",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            })
            ?.AttributeClass
            ?.TypeArguments ?? default;

        if(typeArgs.Length == 0)
            return default;

        TypeModel[] arr = new TypeModel[typeArgs.Length];

        for (int i = 0; i < typeArgs.Length; i++)
        {
            arr[i] = new TypeModel(
                typeArgs[i].IsValueType,
                typeArgs[i].ToDisplayString(FullyQualifiedTypeNameFormat)
                );
        }

        return new ValueUnionModel(
            typeArgs.Length, 
            typeSymbol.ContainingNamespace is { IsGlobalNamespace: true } ? null : typeSymbol.ContainingNamespace.ToString(),
            ctx.TargetSymbol.Name,
            ctx.TargetSymbol.ToDisplayString(TypeNameFormat),
            new(arr));
    }

    private static SourceOutput CreateSource(ValueUnionModel model, CancellationToken ct)
    {
        CodeBuilder codeBuilder = CodeBuilder
            .ThreadShared
            .If(model.Namespace is not null, model.Namespace, (n, c) => c.Append("namespace ").Append(n).AppendLine(";"))
            .AppendLine()
            .AppendLine("[global::System.Runtime.CompilerServices.Union]")
            .AppendLine("[global::System.Runtime.InteropServices.StructLayout(global::System.Runtime.InteropServices.LayoutKind.Auto)]")
            .Append("partial struct ").Append(model.Name).AppendLine(" : global::System.Runtime.CompilerServices.IUnion")
            .Scope()
                .AppendLine("private readonly byte _tag;")
                // fields
                .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                    cb.Append("private readonly ")
                    .Append(type.FullName).Append(" _value")
                    .Append(i + 1)
                    .AppendLine(";"))
                .AppendLine()
                // ctors
                .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                    cb.Append("public ").Append(model.Name).Append("(").Append(type.FullName).AppendLine(" value)")
                        .Scope()
                            .Append("_value").Append(i + 1).AppendLine(" = value;")
                            .Append("_tag = ").Append(i + 1).AppendLine(";")
                        .Unscope())
                .AppendLine()
                .AppendLine("public readonly object? Value => _tag switch")
                .Scope()
                    // Value
                    .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                        cb.Append(i + 1).Append(" => _value").Append(i + 1).AppendLine(","))
                    .AppendLine("_ => null,")
                .Unscope()
                .AppendLine(";")
                .AppendLine()
                // TryGet
                .Foreach<TypeModel>(model.Types, ct, (in type, cb, i) =>
                    cb.Append("public readonly bool TryGetValue(out ").Append(type.FullName).AppendLine(" value)")
                        .Scope()
                            .Append("value = _value").Append(i + 1).AppendLine(";")
                            .Append("return _tag == ").Append(i + 1).AppendLine(";")
                        .Unscope())
                .Unscope()
            ;

        return new SourceOutput($"{model.Name}.g.cs", codeBuilder.ToString());
    }

    private const string ValueUnionAttributeSource =
        """
        namespace ValueUnion
        {
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1> : global::System.Attribute
            {

            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1, T2> : global::System.Attribute
            {

            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1, T2, T3> : global::System.Attribute
            {

            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1, T2, T3, T4> : global::System.Attribute
            {

            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1, T2, T3, T4, T5> : global::System.Attribute
            {

            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1, T2, T3, T4, T5, T6> : global::System.Attribute
            {

            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1, T2, T3, T4, T5, T6, T7> : global::System.Attribute
            {

            }
            [global::System.AttributeUsage(global::System.AttributeTargets.Struct)]
            internal sealed class ValueUnionAttribute<T1, T2, T3, T4, T5, T6, T7, T8> : global::System.Attribute
            {

            }
        }
        """;
}
