using System.Collections.Immutable;
using System.Linq;
using System.Text;
using UnionStruct.Unions;

namespace UnionStruct.CodeGeneration;

public static class UnionCodeGenerator
{
    public static string GenerateUnionPartialImplementation(UnionDescriptor descriptor)
    {
        var unionContext = UnionContext.Create(descriptor);
        
        var generatedEnum = EnumDeclarationGenerator.Generate(unionContext);
        var generatedInits = InitializationGenerator.Generate(unionContext);
        var generatedChecks = CheckGenerators.Generate(unionContext, generatedEnum);

        var structDeclaration = $"public readonly partial struct {unionContext.FullUnionDeclaration}";

        var foldMethodDeclaration = GenerateFold(descriptor.Fields, unionContext.FieldNameToEnumMap, unionContext.UnvaluedEnums);

        var mapMethodsDeclaration = GenerateMapMethods(
            descriptor.Fields,
            unionContext.FieldNameToEnumMap,
            unionContext.UnvaluedEnums,
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
                                 + generatedEnum.Body + "\n\n"
                                 + layoutStructDeclaration + "\n"
                                 + structDeclaration + "\n"
                                 + "{" + "\n"
                                 + generatedInits.Body + "\n"
                                 + generatedEnum.EnumPropertyDeclaration + "\n"
                                 + generatedChecks.Body + "\n"
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

    private static string FormatDefaultExcept(string init, int paramCount, int index, string stateValue)
    {
        var @params = Enumerable.Range(0, paramCount - 1).Select<int, object>(x => x == index ? "arg" : "default")
            .Append(stateValue).ToArray();
        return string.Format(init, @params);
    }
}