using System;
using System.Collections.Immutable;
using System.Linq;
using UnionStruct.Unions;

namespace UnionStruct.CodeGeneration;

public static class UnionCodeGenerator
{
    public static string GenerateUnionPartialImplementation(UnionDescriptor descriptor)
    {
        const string stateConfig = nameof(UnionPartAttribute.State);

        var enumMap = descriptor.Fields
            .ToImmutableDictionary(
                x => x.Name,
                x => x.UnionArguments.TryGetValue(stateConfig, out var stateName)
                    ? SanitizeState(stateName)
                    : SanitizeField(x.Name)
            );

        var enumValues = string.Join(',', enumMap.Values);

        var genericParameters = string.Join(',', descriptor.GenericParameters);
        var genericDeclaration = $"<{genericParameters}>";

        var enumDeclaration = $"public enum {descriptor.StructName}State {{ {enumValues} }}";

        var enumPropertyDeclaration = $"public {descriptor.StructName}State State {{ get; }}";

        var structDeclaration = $"public readonly partial struct {descriptor.StructName}{genericDeclaration}";

        var fieldsTuple = $"({string.Join(',', descriptor.Fields.Select(f => f.Name).Append("State"))})";

        var paramsCount = descriptor.Fields.Length + 1;
        var fieldsTupleInit = $"({string.Join(',', Enumerable.Range(0, paramsCount).Select(x => $"{{{x}}}"))})";

        var constructorsDeclaration = string.Join('\n', descriptor.Fields.Select(
            (field, i) =>
                $"private {descriptor.StructName}({field.Type} arg) => {fieldsTuple} = {FormatDefaultExcept(fieldsTupleInit, paramsCount, i, $"{descriptor.StructName}State.{enumMap[field.Name]}")};"
        ));

        var staticMethodsDeclaration = string.Join('\n', descriptor.Fields.Select(
            field =>
                $"public static {descriptor.StructName}{genericDeclaration} As{enumMap[field.Name]}({field.Type} arg) => new(arg);"
        ));

        var methodsDeclaration = string.Join("\n",
            descriptor.Fields.Select(f => GenerateTryGet(f, enumMap[f.Name], $"{descriptor.StructName}State"))
        );

        var namespaceDeclaration = $"namespace {descriptor.Namespace ?? $"UnionStruct.Generated.{descriptor.StructName}"};";
        var usingsDeclaration = string.Join("\n", [
            "using System.Diagnostics.CodeAnalysis;"
        ]);

        var nullableDeclaration = "#nullable enable";

        return usingsDeclaration + "\n\n"
                                 + nullableDeclaration + "\n\n"
                                 + namespaceDeclaration + "\n\n"
                                 + enumDeclaration + "\n\n"
                                 + structDeclaration + "\n"
                                 + "{" + "\n"
                                 + constructorsDeclaration + "\n\n"
                                 + enumPropertyDeclaration + "\n\n"
                                 + methodsDeclaration + "\n\n"
                                 + staticMethodsDeclaration + "\n\n"
                                 + "}" + "\n";
    }

    private static string GenerateTryGet(UnionTypeDescriptor descriptor, string stateEnumName, string enumType)
    {
        const string returnTrueStatement = "return true;";
        const string returnFalseStatement = "return false;";

        var methodDeclaration = $"public bool Is{stateEnumName}([NotNullWhen(true)] out {descriptor.Type}? value)";
        var ifDeclaration = $"if (State == {enumType}.{stateEnumName})";
        var ifBody = $"value = {descriptor.Name}!;";
        const string elseBody = "value = default;";

        return methodDeclaration + "\n"
                                 + "{" + "\n"
                                 + ifDeclaration + "\n"
                                 + "{" + "\n"
                                 + ifBody + "\n"
                                 + returnTrueStatement + "\n"
                                 + "}" + "\n"
                                 + elseBody + "\n"
                                 + returnFalseStatement + "\n"
                                 + "}" + "\n";
    }

    private static string SanitizeState(string state)
    {
        var span = state.AsSpan();
        var hasQuotes = span.StartsWith("\"") && span.EndsWith("\"");

        if (hasQuotes)
        {
            span = span[1..^1];
        }

        return span.ToString();
    }

    private static string SanitizeField(string field)
    {
        var span = field.AsSpan();
        var hasDash = span.StartsWith("_");

        if (hasDash)
        {
            span = span[1..];
        }

        var isLowerStart = char.IsLower(span[0]);

        if (isLowerStart)
        {
            Span<char> copySpan = stackalloc char[span.Length];
            span.CopyTo(copySpan);
            copySpan[0] = char.ToUpper(copySpan[0]);

            span = new ReadOnlySpan<char>(copySpan.ToArray());
        }

        return span.ToString();
    }

    private static string FormatDefaultExcept(string init, int paramCount, int index, string stateValue)
    {
        var @params = Enumerable.Range(0, paramCount - 1).Select<int, object>(x => x == index ? "arg" : "default")
            .Append(stateValue).ToArray();
        return string.Format(init, @params);
    }
}