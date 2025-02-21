using System.Linq;
using System.Text;
using UnionStruct.Unions;

namespace UnionStruct.CodeGeneration;

public static class ExtensionsGenerator
{
    public static string Generate(UnionContext unionContext)
    {
        return $$"""
                 public static class {{unionContext.Descriptor.StructName}}GeneratedExtensions
                 {
                    {{GenerateAsyncMapExtensions(unionContext)}}
                 }
                 """;
    }

    private static string GenerateAsyncMapExtensions(UnionContext context)
    {
        var descriptors = context.Descriptor.Fields;
        var stateEnumMap = context.FieldNameToEnumMap;
        var structName = context.Descriptor.StructName;
        var genericParams = context.Descriptor.GenericParameters;
        var fullStructName = context.FullUnionDeclaration;
        var initialGenerics = string.Join(",", context.Descriptor.GenericParameters);
        initialGenerics = initialGenerics == string.Empty ? string.Empty : $"{initialGenerics}, ";

        var allConstraints = string.Join('\n', context.Descriptor.GenericConstraints.Values);

        var sb = new StringBuilder();

        var query = descriptors
            .Where(x => x.UnionArguments.TryGetValue(nameof(UnionPartAttribute.AddMap), out var value) && value == "true")
            .Select((x, i) => (x, i, genericParams.Contains(x.Type)));

        foreach (var (descriptor, index, isGeneric) in query)
        {
            var stateName = stateEnumMap[descriptor.Name];

            var genericPlaceHolders = string.Join(",", Enumerable.Range(0, genericParams.Length).Select(x => $"{{{x}}}"));
            var outcomeParams = string.Format(genericPlaceHolders,
                genericParams.Select<string, object>((x, i) => isGeneric && i == index ? "TOut" : x).ToArray());
            var genericFormat = $"<{outcomeParams}>";
            
            var genericConstraint =
                isGeneric && context.Descriptor.GenericConstraints.TryGetValue(descriptor.Type, out var constraint)
                    ? constraint.Replace(descriptor.Type, "TOut")
                    : string.Empty;

            var methodConstraints = allConstraints + '\n' + genericConstraint;

            var newStructType = $"{structName}{(genericFormat == "<>" ? string.Empty : genericFormat)}";
            var funcType = isGeneric ? "TOut" : descriptor.Type;
            var genericType = isGeneric ? "TOut" : string.Empty;
            
            var parameters = $"<{initialGenerics}{genericType}>";
            parameters = parameters == "<>" ? string.Empty : parameters;

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}{{parameters}}(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, {{funcType}}> mapper)
                        {{methodConstraints}}
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: null } => t.Result.Map{{stateName}}(mapper),
                                { Exception: not null } => throw new InvalidOperationException("Error when mapping", innerException: t.Exception),
                                _ => throw new InvalidOperationException("Error when mapping") 
                            }
                        );
                  }
                  """
            );

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}{{parameters}}(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, {{newStructType}}> mapper)
                        {{methodConstraints}}
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: null } => t.Result.Map{{stateName}}(mapper),
                                { Exception: not null } => throw new InvalidOperationException("Error when mapping", innerException: t.Exception),
                                _ => throw new InvalidOperationException("Error when mapping") 
                            }
                        );
                  }
                  """
            );

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}Async{{parameters}}(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, Task<{{funcType}}>> mapper)
                        {{methodConstraints}}
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: null } => t.Result.Map{{stateName}}Async(mapper),
                                { Exception: not null } => Task.FromException<{{newStructType}}>(new InvalidOperationException("Error when mapping", innerException: t.Exception)),
                                _ => Task.FromException<{{newStructType}}>(new InvalidOperationException("Error when mapping"))
                            }
                        ).Unwrap();
                  }
                  """
            );

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}Async{{parameters}}(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, Task<{{newStructType}}>> mapper)
                        {{methodConstraints}}
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: null } => t.Result.Map{{stateName}}Async(mapper),
                                { Exception: not null } => Task.FromException<{{newStructType}}>(new InvalidOperationException("Error when mapping", innerException: t.Exception)),
                                _ => Task.FromException<{{newStructType}}>(new InvalidOperationException("Error when mapping"))
                            }
                        ).Unwrap();
                  }
                  """
            );
        }

        return sb.ToString();
    }
}