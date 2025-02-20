using System.Linq;
using System.Text;
using UnionStruct.Unions;

namespace UnionStruct.CodeGeneration;

public static class ExtensionsGenerator
{
    public static string Generate(UnionContext unionContext)
    {
        return $$"""
                 public static class {{unionContext.Descriptor.StructName}}Extensions 
                 {
                    {{GenerateAsyncExtensions(unionContext)}}
                 }
                 """;
    }

    private static string GenerateAsyncExtensions(UnionContext context)
    {
        var descriptors = context.Descriptor.Fields;
        var stateEnumMap = context.FieldNameToEnumMap;
        var structName = context.Descriptor.StructName;
        var genericParams = context.Descriptor.GenericParameters;
        var fullStructName = context.FullUnionDeclaration;
        var initialGenerics = string.Join(",", context.Descriptor.GenericParameters);

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

            var newStructType = $"{structName}{genericFormat}";

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}<{{initialGenerics}}, TOut>(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, TOut> mapper)
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: not null } => t.Result.Map{{stateName}}(mapper),
                                { Exception: null } => throw new InvalidOperationException("Error when mapping", innerException: t.Exception),
                                _ => throw new InvalidOperationException("Error when mapping") 
                            }
                        );
                  }
                  """
            );

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}<{{initialGenerics}}, TOut>(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, {{newStructType}}> mapper)
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: not null } => t.Result.Map{{stateName}}(mapper),
                                { Exception: null } => throw new InvalidOperationException("Error when mapping", innerException: t.Exception),
                                _ => throw new InvalidOperationException("Error when mapping") 
                            }
                        );
                  }
                  """
            );

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}Async<{{initialGenerics}}, TOut>(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, Task<TOut>> mapper)
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: not null } => t.Result.Map{{stateName}}Async(mapper),
                                { Exception: null } => Task.FromException<{{newStructType}}>(new InvalidOperationException("Error when mapping", innerException: t.Exception)),
                                _ => Task.FromException<{{newStructType}}>(new InvalidOperationException("Error when mapping"))
                            }
                        ).Unwrap();
                  }
                  """
            );

            sb.AppendLine(
                $$"""
                  public static Task<{{newStructType}}> Map{{stateName}}Async<{{initialGenerics}}, TOut>(this Task<{{fullStructName}}> src, Func<{{descriptor.Type}}, Task<{{newStructType}}>> mapper)
                  {
                        return src.ContinueWith(
                            t => t switch 
                            {
                                { Exception: not null } => t.Result.Map{{stateName}}Async(mapper),
                                { Exception: null } => Task.FromException<{{newStructType}}>(new InvalidOperationException("Error when mapping", innerException: t.Exception)),
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