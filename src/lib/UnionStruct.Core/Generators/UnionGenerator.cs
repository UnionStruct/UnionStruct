using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace UnionStruct.Generators;

[Generator]
public class UnionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var declaredEnums = context.Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<EnumDeclarationSyntax>()
            .Select(tree => new
            {
                DeclaredEnumType = tree.Identifier.Text,
                DeclaredEnumValues = tree.Members.Select(x => x.Identifier.Text).ToImmutableArray()
            })
            .ToImmutableDictionary(x => x.DeclaredEnumType, x => x.DeclaredEnumValues);

        var types = context.Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes())
            .OfType<StructDeclarationSyntax>()
            .Where(tree => tree.Modifiers.Any(m => m.Text == "partial")
                           && tree.AttributeLists.Any(a => a.Attributes.Any(aa => aa.Name.ToString().StartsWith("Union<"))))
            .Select(x => new
            {
                StructName = x.Identifier.Text,
                UnionType = x.AttributeLists
                    .SelectMany(a => a.Attributes.Select(aa => aa.Name.ToString()))
                    .Where(name => name.StartsWith("Union<"))
                    .Select(Extensions.ExtractUnionType)
                    .Single()
            })
            .ToImmutableDictionary(x => x.StructName, x => x.UnionType);

        var unionTypes = types.Join(
            declaredEnums,
            x => x.Value,
            x => x.Key,
            (x, y) => new UnionDescriptor(x.Key, y.Value)
        ).ToImmutableArray();
    }
}

file static class Extensions
{
    public static string ExtractUnionType(string declaration)
    {
        var span = declaration.AsSpan();
        var openBracket = declaration.IndexOf('<') + 1;
        var slice = span[openBracket..];
        var slice2 = slice[..^1];

        return slice2.ToString();
    }
}