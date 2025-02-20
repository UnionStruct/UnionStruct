using System.Linq;
using UnionStruct.Unions;

namespace UnionStruct.CodeGeneration;

public static class CheckGenerators
{
    public static GeneratedChecks Generate(UnionContext context, GeneratedEnum generatedEnum)
    {
        var methodsDeclaration = string.Join(
            "\n",
            context.Descriptor.Fields.Select(
                f => GenerateTryGet(f, context.FieldNameToEnumMap[f.Name], generatedEnum.Name,
                    context.FullUnionDeclaration)
            )
        );

        if (context.UnvaluedEnums.Length == 0)
        {
            return new GeneratedChecks(methodsDeclaration);
        }

        var unvaluedStateMethodsDeclaration = string.Join("\n", context.UnvaluedEnums.Select(x =>
            $"public bool Is{x}() => State == {generatedEnum.Name}.{x};"));

        var unvaluedWhenMethodsDeclaration = string.Join("\n", context.UnvaluedEnums.Select(x =>
                $$"""
                  public {{context.FullUnionDeclaration}} When{{x}}(Action body)
                  {
                     if (State == {{generatedEnum.Name}}.{{x}})
                     {
                         body();
                     }
                     
                     return this;
                  }
                  """
            )
        );

        return new GeneratedChecks(
            methodsDeclaration + "\n"
                               + unvaluedStateMethodsDeclaration + "\n\n"
                               + unvaluedWhenMethodsDeclaration
        );
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
}

public readonly struct GeneratedChecks
{
    public GeneratedChecks(string body)
    {
        Body = body;
    }

    public string Body { get; }
}