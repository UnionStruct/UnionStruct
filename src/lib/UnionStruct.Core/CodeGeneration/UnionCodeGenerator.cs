using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
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

        var unvaluedEnums = descriptor.UnvaluedStates.Select(SanitizeState).ToImmutableArray();

        var enumValues = string.Join(',', enumMap.Values.Concat(unvaluedEnums));

        var genericParameters = string.Join(',', descriptor.GenericParameters);
        var genericDeclaration = genericParameters == string.Empty ? string.Empty : $"<{genericParameters}>";

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

        var stateConstructorDeclaration = string.Empty;
        var unvaluedStateStaticMembersDeclaration = string.Empty;
        var unvaluedStateMethodsDeclaration = string.Empty;
        if (unvaluedEnums.Length != 0)
        {
            stateConstructorDeclaration =
                $"private {descriptor.StructName}({descriptor.StructName}State arg) => {fieldsTuple} = {FormatDefaultExcept(fieldsTupleInit, paramsCount, paramsCount + 1, "arg")};";

            unvaluedStateStaticMembersDeclaration = string.Join("\n", unvaluedEnums.Select(x =>
                $"public static readonly {descriptor.StructName}{genericDeclaration} {x} = new({descriptor.StructName}State.{x});"));

            unvaluedStateMethodsDeclaration = string.Join("\n", unvaluedEnums.Select(x =>
                $"public bool Is{x}() => State == {descriptor.StructName}State.{x};"));

            var unvaluedIfStateMethodsDeclaration = string.Join("\n", unvaluedEnums.Select(
                x =>
                    $"public {descriptor.StructName}{genericDeclaration} If{x}(Action body) {{ if (State == {descriptor.StructName}State.{x}) {{ body(); }} return this; }}"
            ));

            unvaluedStateMethodsDeclaration += "\n" + unvaluedIfStateMethodsDeclaration;
        }

        var staticMethodsDeclaration = string.Join('\n', descriptor.Fields.Select(
            field =>
                $"public static {descriptor.StructName}{genericDeclaration} {enumMap[field.Name]}({field.Type} arg) => new(arg);"
        ));

        var methodsDeclaration = string.Join("\n",
            descriptor.Fields.Select(f => GenerateTryGet(f, enumMap[f.Name], $"{descriptor.StructName}State",
                $"{descriptor.StructName}{genericDeclaration}"))
        );

        var foldMethodDeclaration = GenerateFold(descriptor.Fields, enumMap, unvaluedEnums);

        var mapMethodsDeclaration = GenerateMapMethods(
            descriptor.Fields,
            enumMap,
            unvaluedEnums,
            descriptor.GenericParameters,
            descriptor.StructName
        );

        var namespaceDeclaration = $"namespace {descriptor.Namespace ?? $"UnionStruct.Generated.{descriptor.StructName}"};";

        var usingsDeclaration = string.Join("\n", descriptor.Usings.Select(x => $"using {x};").Concat([
            "using System.Diagnostics.CodeAnalysis;",
            "using System.Runtime.InteropServices;"
        ]));

        const string nullableDeclaration = "#nullable enable";
        const string layoutStructDeclaration = "[StructLayout(LayoutKind.Auto)]";

        return usingsDeclaration + "\n\n"
                                 + nullableDeclaration + "\n\n"
                                 + namespaceDeclaration + "\n\n"
                                 + enumDeclaration + "\n\n"
                                 + layoutStructDeclaration + "\n"
                                 + structDeclaration + "\n"
                                 + "{" + "\n"
                                 + unvaluedStateStaticMembersDeclaration + "\n\n"
                                 + constructorsDeclaration + "\n\n"
                                 + stateConstructorDeclaration + "\n\n"
                                 + enumPropertyDeclaration + "\n\n"
                                 + methodsDeclaration + "\n\n"
                                 + unvaluedStateMethodsDeclaration + "\n\n"
                                 + foldMethodDeclaration + "\n\n"
                                 + mapMethodsDeclaration + "\n\n"
                                 + staticMethodsDeclaration + "\n\n"
                                 + "}" + "\n";
    }

    private static string GenerateMapMethods(
        ImmutableArray<UnionTypeDescriptor> descriptors,
        ImmutableDictionary<string, string> stateEnumMap,
        ImmutableArray<string> unvaluedEnums,
        ImmutableArray<string> genericParams,
        string structName
    )
    {
        var sb = new StringBuilder();

        var query = descriptors
            .Where(x => x.UnionArguments.TryGetValue(nameof(UnionPartAttribute.AddMap), out var value) && value == "true")
            .Select((x, i) => (x, i, genericParams.Contains(x.Type)));

        foreach (var (descriptor, index, isGeneric) in query)
        {
            var genericPlaceHolders = string.Join(",", Enumerable.Range(0, genericParams.Length).Select(x => $"{{{x}}}"));
            var genericFormat = $"<{genericPlaceHolders}>";
            var outcomeParams = string.Format(genericFormat,
                genericParams.Select<string, object>((x, i) => isGeneric && i == index ? "TOut" : x).ToArray());

            var methodDeclaration =
                $"public {structName}{outcomeParams} Map{stateEnumMap[descriptor.Name]}<TOut>(Func<{descriptor.Type}, TOut> mapper)";

            var mainSwitch =
                $"_ when Is{stateEnumMap[descriptor.Name]}(out var x) => {structName}{outcomeParams}.{stateEnumMap[descriptor.Name]}(mapper(x)),";

            var restSwitch = string.Join('\n', descriptors
                .Where(x => x.Name != descriptor.Name)
                .Select(x =>
                    $"_ when Is{stateEnumMap[x.Name]}(out var x) =>{structName}{outcomeParams}.{stateEnumMap[x.Name]}(x),")
                .Concat(unvaluedEnums.Select(x => $"_ when Is{x}() => {structName}{outcomeParams}.{x},"))
                .Append("_ => throw new NotImplementedException()"));

            sb.Append(methodDeclaration).AppendLine(" => this switch")
                .AppendLine("{")
                .AppendLine(mainSwitch)
                .AppendLine(restSwitch)
                .AppendLine("};");

            var bindMethodDeclaration =
                $"public {structName}{outcomeParams} Map{stateEnumMap[descriptor.Name]}<TOut>(Func<{descriptor.Type}, {structName}{outcomeParams}> mapper)";

            var bindMainSwitch =
                $"_ when Is{stateEnumMap[descriptor.Name]}(out var x) => mapper(x),";

            var bindRestSwitch = string.Join('\n', descriptors
                .Where(x => x.Name != descriptor.Name)
                .Select(x =>
                    $"_ when Is{stateEnumMap[x.Name]}(out var x) =>{structName}{outcomeParams}.{stateEnumMap[x.Name]}(x),")
                .Concat(unvaluedEnums.Select(x => $"_ when Is{x}() => {structName}{outcomeParams}.{x},"))
                .Append("_ => throw new NotImplementedException()"));

            sb.AppendLine().Append(bindMethodDeclaration).AppendLine(" => this switch")
                .AppendLine("{")
                .AppendLine(bindMainSwitch)
                .AppendLine(bindRestSwitch)
                .AppendLine("};");

            var asyncMethodDeclaration =
                $"public Task<{structName}{outcomeParams}> Map{stateEnumMap[descriptor.Name]}<TOut>(Func<{descriptor.Type}, Task<TOut>> mapper)";

            var asyncMainSwitch =
                $"_ when Is{stateEnumMap[descriptor.Name]}(out var x) => mapper(x).ContinueWith(t => t switch {{ {{ Exception: null }} => {structName}{outcomeParams}.{stateEnumMap[descriptor.Name]}(t.Result), _ => throw new InvalidOperationException(\"Error when mapping\") }}),";

            var asyncRestSwitch = string.Join('\n', descriptors
                .Where(x => x.Name != descriptor.Name)
                .Select(x =>
                    $"_ when Is{stateEnumMap[x.Name]}(out var x) => Task.FromResult({structName}{outcomeParams}.{stateEnumMap[x.Name]}(x)),")
                .Concat(unvaluedEnums.Select(x => $"_ when Is{x}() => Task.FromResult({structName}{outcomeParams}.{x}),"))
                .Append($"_ => Task.FromException<{structName}{outcomeParams}>(new NotImplementedException())"));

            sb.AppendLine().Append(asyncMethodDeclaration).AppendLine(" => this switch")
                .AppendLine("{")
                .AppendLine(asyncMainSwitch)
                .AppendLine(asyncRestSwitch)
                .AppendLine("};");

            var asyncBindMethodDeclaration =
                $"public Task<{structName}{outcomeParams}> Map{stateEnumMap[descriptor.Name]}<TOut>(Func<{descriptor.Type}, Task<{structName}{outcomeParams}>> mapper)";

            var asyncBindMainSwitch =
                $"_ when Is{stateEnumMap[descriptor.Name]}(out var x) => mapper(x),";

            var asyncBindRestSwitch = string.Join('\n', descriptors
                .Where(x => x.Name != descriptor.Name)
                .Select(x =>
                    $"_ when Is{stateEnumMap[x.Name]}(out var x) => Task.FromResult({structName}{outcomeParams}.{stateEnumMap[x.Name]}(x)),")
                .Concat(unvaluedEnums.Select(x => $"_ when Is{x}() => Task.FromResult({structName}{outcomeParams}.{x}),"))
                .Append($"_ => Task.FromException<{structName}{outcomeParams}>(new NotImplementedException())"));

            sb.AppendLine().Append(asyncBindMethodDeclaration).AppendLine(" => this switch")
                .AppendLine("{")
                .AppendLine(asyncBindMainSwitch)
                .AppendLine(asyncBindRestSwitch)
                .AppendLine("};");
        }

        return sb.ToString();
    }

    private static string GenerateFold(
        ImmutableArray<UnionTypeDescriptor> descriptors,
        ImmutableDictionary<string, string> stateEnumMap,
        ImmutableArray<string> unvaluedEnums
    )
    {
        var funcParameters = string.Join(',',
            descriptors.Select(x => $"Func<{x.Type}, TOut> {stateEnumMap[x.Name]}")
                .Concat(unvaluedEnums.Select(x => $"Func<TOut> {x}"))
        );

        var functionDeclaration = $"public TOut Fold<TOut>({funcParameters})";

        var switchParts = string.Join('\n', descriptors
            .Select(x => $"_ when Is{stateEnumMap[x.Name]}(out var x) => {stateEnumMap[x.Name]}(x),")
            .Concat(unvaluedEnums.Select(x => $"_ when Is{x}() => {x}(),"))
            .Append("_ => throw new NotImplementedException()"));

        var switchFunction = $"{functionDeclaration} => this switch" + "\n"
                                                                     + "{" + "\n"
                                                                     + switchParts + "\n"
                                                                     + "};" + "\n";

        return switchFunction;
    }

    private static string GenerateTryGet(UnionTypeDescriptor descriptor, string stateEnumName, string enumType,
        string fullStructType)
    {
        const string returnTrueStatement = "return true;";
        const string returnFalseStatement = "return false;";

        var methodDeclaration = $"public bool Is{stateEnumName}([NotNullWhen(true)] out {descriptor.Type}? value)";
        var ifDeclaration = $"if (State == {enumType}.{stateEnumName})";
        var ifBody = $"value = {descriptor.Name}!;";
        const string elseBody = "value = default;";

        var isMethod = methodDeclaration + "\n"
                                         + "{" + "\n"
                                         + ifDeclaration + "\n"
                                         + "{" + "\n"
                                         + ifBody + "\n"
                                         + returnTrueStatement + "\n"
                                         + "}" + "\n"
                                         + elseBody + "\n"
                                         + returnFalseStatement + "\n"
                                         + "}" + "\n";

        var ifMethodBody = $"if (Is{stateEnumName}(out var value)) {{ action(value); }} return this;";
        var ifMethodDeclaration = $"public {fullStructType} When{stateEnumName}(Action<{descriptor.Type}> action)" + "\n"
            + "{" + "\n"
            + ifMethodBody
            + "}" + "\n";

        return isMethod + "\n"
                        + ifMethodDeclaration + "\n";
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