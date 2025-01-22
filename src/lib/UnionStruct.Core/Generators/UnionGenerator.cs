using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using UnionStruct.CodeGeneration;
using UnionStruct.Unions;

namespace UnionStruct.Generators;

[Generator]
public class UnionGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var types = context.Compilation.SyntaxTrees
            .SelectMany(tree => tree.GetRoot().DescendantNodes()).OfType<StructDeclarationSyntax>()
            .Where(tree => tree.Modifiers.Any(m => m.Text == "partial")
                           && tree.AttributeLists.SelectMany(a => a.Attributes).Any(x => x.Name.ToString() == "Union"))
            .Select(tree => new
            {
                NamespaceTree = Extensions.TraverseBack<BaseNamespaceDeclarationSyntax>(tree),
                Usings = Extensions.TraverseBack<CompilationUnitSyntax>(tree)?.Usings.Select(x => x.Name.ToFullString())
                    .ToImmutableArray(),
                StructTree = tree,
                FieldsTree = tree.DescendantNodes().OfType<FieldDeclarationSyntax>()
                    .Where(field =>
                        field.AttributeLists.SelectMany(a => a.Attributes).Any(x => x.Name.ToString() == "UnionPart"))
                    .ToList()
            })
            .Select(trees => new UnionDescriptor(
                trees.NamespaceTree?.Name.ToString(),
                trees.StructTree.Identifier.ToString(),
                trees.StructTree.DescendantNodes().OfType<TypeParameterSyntax>().Select(x => x.Identifier.ToString())
                    .ToImmutableArray(),
                trees.StructTree.AttributeLists.SelectMany(x => x.Attributes)
                    .Where(a => a.ToString().StartsWith("Union"))
                    .SelectMany(a =>
                        a.ArgumentList?.Arguments.SelectMany(arg => Extensions.ParseUnvaluedStates(arg.ToString()))
                        ?? []).ToImmutableArray(),
                trees.Usings ?? ImmutableArray<string>.Empty,
                trees.FieldsTree.Select(f => new UnionTypeDescriptor(
                    f.Declaration.Variables.Single().Identifier.ToString(),
                    f.DescendantNodes().OfType<TypeSyntax>().Last().ToString(),
                    f.AttributeLists.SelectMany(a => a.Attributes)
                        .Where(a => a.ToString().StartsWith("UnionPart"))
                        .SelectMany(a =>
                            a.ArgumentList?.Arguments.Select(arg => Extensions.ParseArgumentSyntax(arg.ToString()))
                            ?? []
                        )
                        .ToImmutableDictionary(x => x.Argument, x => x.Value)
                )).ToImmutableArray()
            ))
            .ToImmutableArray();

        foreach (var unionDescriptor in types)
        {
            var code = UnionCodeGenerator.GenerateUnionPartialImplementation(unionDescriptor);

            var parsedCode = CSharpSyntaxTree.ParseText(code);
            var formattedCode = parsedCode.GetRoot().NormalizeWhitespace().ToFullString();

            context.AddSource($"{unionDescriptor.StructName}.Generated.cs", formattedCode);
        }
    }
}

file static class Extensions
{
    public static T? TraverseBack<T>(SyntaxNode syntaxTree) where T : SyntaxNode
    {
        var node = syntaxTree.Parent;
        while (node is not (null or T))
        {
            node = node?.Parent;
        }

        return node as T;
    }

    public static (string Argument, string Value) ParseArgumentSyntax(string syntax)
    {
        var span = syntax.AsSpan();
        var index = span.IndexOf('=');
        var hasSpaceBefore = span.IndexOf(' ') is { } x && x + 1 == index;
        var hasSpaceAfter = span.LastIndexOf(' ') is { } y && y - 1 == index;

        var argumentName = span[..(index - (hasSpaceBefore ? 1 : 0))];
        var argumentValue = span[(index + 1 + (hasSpaceAfter ? 1 : 0))..];

        return (argumentName.ToString(), argumentValue.ToString());
    }

    public static IEnumerable<string> ParseUnvaluedStates(string syntax) =>
        syntax.Split(",", StringSplitOptions.RemoveEmptyEntries);
}