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

        var unvaluedMethodsDeclaration = string.Join("\n", context.UnvaluedEnums.Select(x =>
                $$"""
                  public bool Is{{x}}() => State == {{generatedEnum.Name}}.{{x}};

                  public {{context.FullUnionDeclaration}} When{{x}}(Action body)
                  {
                     if (State == {{generatedEnum.Name}}.{{x}})
                     {
                         body();
                     }
                     
                     return this;
                  }
                  
                  public Task<{{context.FullUnionDeclaration}}> When{{x}}Async(Func<Task> body)
                  {
                     if (State == {{generatedEnum.Name}}.{{x}})
                     {
                         var toReturn = this;
                         return body().ContinueWith(
                            t => t switch 
                            {
                                { Exception: null } => toReturn,
                                { Exception: not null } => throw new InvalidOperationException("Error when observing state {{x}}", innerException: t.Exception),
                                _ => throw new InvalidOperationException("Error when observing state {{x}}")
                            }
                         );
                     }
                     
                     return Task.FromResult(this);
                  }
                  """
            )
        );

        return new GeneratedChecks(methodsDeclaration + "\n" + unvaluedMethodsDeclaration);
    }

    private static string GenerateTryGet(
        UnionTypeDescriptor descriptor,
        string stateEnumName,
        string enumType,
        string fullStructType
    )
    {
        return $$"""
                 public bool Is{{stateEnumName}}([NotNullWhen(true)] out {{descriptor.Type}}? value) 
                 {
                    if (State == {{enumType}}.{{stateEnumName}}) 
                    {
                        value = {{descriptor.Name}}!;
                        return true;
                    }
                    
                    value = default;
                    return false;
                 }

                 public {{fullStructType}} When{{stateEnumName}}(Action<{{descriptor.Type}}> action) 
                 {
                    if (Is{{stateEnumName}}(out var value)) 
                    {
                        action(value{{(descriptor.IsNullable ? ".Value" : string.Empty)}});
                    }
                    
                    return this;
                 }
                 
                  public Task<{{fullStructType}}> When{{stateEnumName}}Async(Func<{{descriptor.Type}}, Task> action)
                  {
                     if (Is{{stateEnumName}}(out var value))
                     {
                         var toReturn = this;
                         return action(value{{(descriptor.IsNullable ? ".Value" : string.Empty)}}).ContinueWith(
                            t => t switch 
                            {
                                { Exception: null } => toReturn,
                                { Exception: not null } => throw new InvalidOperationException("Error when observing state {{stateEnumName}}", innerException: t.Exception),
                                _ => throw new InvalidOperationException("Error when observing state {{stateEnumName}}")
                            }
                         );
                     }
                     
                     return Task.FromResult(this);
                  }
                 """;
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