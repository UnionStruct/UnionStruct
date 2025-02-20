using System.Linq;

namespace UnionStruct.CodeGeneration;

public static class InitializationGenerator
{
    public static GeneratedInitializators Generate(UnionContext context)
    {
        var fieldsTuple = $"({string.Join(',', context.Descriptor.Fields.Select(f => f.Name).Append("State"))})";

        var paramsCount = context.Descriptor.Fields.Length + 1;
        var fieldsTupleInit = $"({string.Join(',', Enumerable.Range(0, paramsCount).Select(x => $"{{{x}}}"))})";

        var constructorsDeclaration = string.Join('\n', context.Descriptor.Fields.Select(
            (field, i) =>
                $"private {context.Descriptor.StructName}({field.Type} arg) => {fieldsTuple} = {FormatDefaultExcept(fieldsTupleInit, paramsCount, i, $"{context.Descriptor.StructName}State.{context.FieldNameToEnumMap[field.Name]}")};"
        ));

        var staticMethodsDeclaration = string.Join('\n', context.Descriptor.Fields.Select(
            field =>
                $"public static {context.FullUnionDeclaration} {context.FieldNameToEnumMap[field.Name]}({field.Type} arg) => new(arg);"
        ));

        if (context.UnvaluedEnums.Length == 0)
        {
            return new GeneratedInitializators(constructorsDeclaration + "\n\n" + staticMethodsDeclaration);
        }

        var stateConstructorDeclaration =
            $"private {context.Descriptor.StructName}({context.Descriptor.StructName}State arg) => {fieldsTuple} = {FormatDefaultExcept(fieldsTupleInit, paramsCount, paramsCount + 1, "arg")};";

        var unvaluedStateStaticMembersDeclaration = string.Join("\n", context.UnvaluedEnums.Select(x =>
            $"public static readonly {context.FullUnionDeclaration} {x} = new({context.Descriptor.StructName}State.{x});"));

        var body =
            unvaluedStateStaticMembersDeclaration + "\n\n"
                                                  + constructorsDeclaration + "\n\n"
                                                  + stateConstructorDeclaration + "\n\n"
                                                  + staticMethodsDeclaration + "\n";

        return new GeneratedInitializators(body);
    }

    private static string FormatDefaultExcept(string init, int paramCount, int index, string stateValue)
    {
        var @params = Enumerable.Range(0, paramCount - 1).Select<int, object>(x => x == index ? "arg" : "default")
            .Append(stateValue).ToArray();
        return string.Format(init, @params);
    }
}

public readonly struct GeneratedInitializators
{
    public GeneratedInitializators(string body)
    {
        Body = body;
    }

    public string Body { get; }
}